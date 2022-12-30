using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("ConsoleApp2")]

namespace Enderlook.StateMachine;

/// <summary>
/// Representation of an state machine.
/// </summary>
/// <typeparam name="TState">Type that determines states.</typeparam>
/// <typeparam name="TEvent">Type that determines events.</typeparam>
/// <typeparam name="TRecipient">Type that determines internal data that can be acceded by actions.</typeparam>
public sealed partial class StateMachine<TState, TEvent, TRecipient>
    where TState : notnull
    where TEvent : notnull
{
    private readonly StateMachineFactory<TState, TEvent, TRecipient> flyweight;

    private TRecipient recipient;

    private int currentState;

    private readonly Dictionary<Type, ParameterSlots> parameters = new();
    private SlotsQueue<ParameterSlot> parameterIndexes = new(1);
    private SlotsQueue<(TEvent Event, int ParameterIndex)> queue = new(1);
    private int reservedForRun = -1;
    // -1: is building but has not stored its first parameter yet.
    private int parameterBuilderFirstIndex = -1;
    private int parameterBuilderVersion;

    internal StateMachine(StateMachineFactory<TState, TEvent, TRecipient> flyweight, TRecipient recipient)
    {
        this.flyweight = flyweight;
        this.recipient = recipient;
        currentState = flyweight.InitialState;
    }

    internal StateMachine(StateMachineFactory<TState, TEvent, TRecipient> flyweight)
    {
        this.flyweight = flyweight;
        currentState = flyweight.InitialState;
        recipient = default!;
    }

    internal static StateMachine<TState, TEvent, TRecipient> From(StateMachineFactory<TState, TEvent, TRecipient> flyweight, TRecipient recipient)
    {
        StateMachine<TState, TEvent, TRecipient> stateMachine = new(flyweight, recipient);
        int initialStateOnEntryStart = flyweight.InitialStateOnEntryStart;
        if (initialStateOnEntryStart != -1)
        {
            Debug.Assert(flyweight.StateEvents.Length > initialStateOnEntryStart);
            StateEventUnion[] stateEvents = flyweight.StateEvents;
            for (int i = initialStateOnEntryStart; i < stateEvents.Length; i++)
                stateEvents[i].Invoke(recipient, default);
        }
        return stateMachine;
    }

    /// <summary>
    /// Returns the current (possibly sub) state of this state machine.
    /// </summary>
    public TState CurrentState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if NET5_0_OR_GREATER
            Debug.Assert(flyweight.States.Length > currentState);
            return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(flyweight.States), currentState).state;
#else
            return flyweight.States[currentState].state;
#endif
        }
    }

    /// <summary>
    /// Returns the current state of this state machine.<br/>
    /// If the state is a substate, its parent hierarchy is included.
    /// </summary>
    public ReadOnlySlice<TState> CurrentStateHierarchy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => flyweight.GetHierarchyOfState(currentState);
    }

    /// <summary>
    /// Returns the events that are accepted by the current (possibly sub) state of this state machine.<br/>
    /// May be empty if doesn't accept any other event (is terminal).
    /// </summary>
    public ReadOnlySlice<TEvent> CurrentAcceptedEvents
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => flyweight.GetAcceptedEventsByState(currentState);
    }

    /// <summary>
    /// Creates the builder of an state machine.
    /// </summary>
    /// <returns>Builder of the state machine.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StateMachineBuilder<TState, TEvent, TRecipient> CreateFactoryBuilder() => new();

    /// <summary>
    /// Return the parent state of <paramref name="state"/>, if any.
    /// </summary>
    /// <param name="state">State whose parent is looked for.</param>
    /// <param name="parentState">If returns <see langword="true"/>, this is the parent state of <paramref name="state"/>.</param>
    /// <returns>Whenever <paramref name="state"/> is a substate.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="state"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetParentStateOf(TState state, [NotNullWhen(true)] out TState? parentState)
    {
        if (state is null) ThrowHelper.ThrowArgumentNullException_State();
        return flyweight.ParentStateOf(state, out parentState);
    }

    /// <summary>
    /// Return the parent hierarchy of <paramref name="state"/>, if any.
    /// </summary>
    /// <param name="state">State whose parent hierarchy is looked for.</param>
    /// <returns>Parent hierarchy of <paramref name="state"/>. May be empty if <paramref name="state"/> is not substate of any other state.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="state"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySlice<TState> GetParentHierarchyOf(TState state)
    {
        if (state is null) ThrowHelper.ThrowArgumentNullException_State();
        return flyweight.GetParentHierarchyOfState(state);
    }

    /// <summary>
    /// Returns the events that are accepted by <paramref name="state"/>, if any.
    /// </summary>
    /// <param name="state">State whose accepted events are looked for.</param>
    /// <returns>Accepted events by <paramref name="state"/>. May be empty if <paramref name="state"/> doesn't accept any other event (is terminal).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="state"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySlice<TEvent> GetAcceptedEventsBy(TState state)
    {
        if (state is null) ThrowHelper.ThrowArgumentNullException_State();
        return flyweight.GetAcceptedEventsByState(state);
    }

    /// <summary>
    /// Determines if the <paramref name="state"/> is the current state or superstate of the current state.
    /// </summary>
    /// <param name="state">State to check.</param>
    /// <returns>Whenever the current state if <paramref name="state"/> or a substate of <paramref name="state"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="state"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsInState(TState state)
    {
        if (state is null) ThrowHelper.ThrowArgumentNullException_State();

        if (typeof(TState).IsValueType)
        {
            // Look for the first using CurrentState to avoid possible initialization of lazy.
            if (EqualityComparer<TState>.Default.Equals(CurrentState, state))
                return true;

            ReadOnlySlice<TState> currentStateHierarchy = CurrentStateHierarchy;
            Debug.Assert(EqualityComparer<TState>.Default.Equals(CurrentState, currentStateHierarchy[currentStateHierarchy.Count - 1]));
            if (currentStateHierarchy.Count == 1)
                // Already checked above.
                return false;

            ref TState current = ref currentStateHierarchy.GetUnsafe(0);
            ref TState end = ref Unsafe.Add(ref current, currentStateHierarchy.Count - 1);
            while (Unsafe.IsAddressLessThan(ref current, ref end))
            {
                if (EqualityComparer<TState>.Default.Equals(current, state))
                    return true;
                current = ref Unsafe.Add(ref current, 1);
            }
        }
        else
        {
            EqualityComparer<TState> equalityComparer = EqualityComparer<TState>.Default;

            // Look for the first using CurrentState to avoid possible initialization of lazy.
            if (equalityComparer.Equals(CurrentState, state))
                return true;

            ReadOnlySlice<TState> currentStateHierarchy = CurrentStateHierarchy;
            Debug.Assert(equalityComparer.Equals(CurrentState, currentStateHierarchy[currentStateHierarchy.Count - 1]));
            if (currentStateHierarchy.Count == 1)
                // Already checked above.
                return false;

            ref TState current = ref currentStateHierarchy.GetUnsafe(0);
            ref TState end = ref Unsafe.Add(ref current, currentStateHierarchy.Count - 1);
            while (Unsafe.IsAddressLessThan(ref current, ref end))
            {
                if (equalityComparer.Equals(current, state))
                    return true;
                current = ref Unsafe.Add(ref current, 1);
            }
        }
        return false;
    }

    /// <summary>
    /// Fire an event to the state machine.<br/>
    /// If the state machine is already firing an state, it's enqueued to run after completion of the current event.
    /// </summary>
    /// <param name="event">Event to fire.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="event"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a parameter builder associated with this state machine has not been finalized.</exception>
    public void Fire(TEvent @event)
    {
        if (@event is null) ThrowHelper.ThrowArgumentNullException_Event();
        if (parameterBuilderFirstIndex != -1) ThrowHelper.ThrowInvalidOperationException_AParameterBuilderHasNotBeenFinalized();
        EnqueueAndRunIfNotRunning(@event, -1);
    }

    /// <summary>
    /// Fire an event to the state machine.<br/>
    /// The event won't be enqueued but actually run, ignoring previously enqueued events.<br/>
    /// If subsequent events are enqueued during the execution of the callbacks of this event, they will also be run after the completion of this event.
    /// </summary>
    /// <param name="event">Event to fire.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="event"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a parameter builder associated with this state machine has not been finalized.</exception>
    public void FireImmediately(TEvent @event)
    {
        if (@event is null) ThrowHelper.ThrowArgumentNullException_Event();
        if (parameterBuilderFirstIndex != -1) ThrowHelper.ThrowInvalidOperationException_AParameterBuilderHasNotBeenFinalized();
        EnqueueAndRun(@event, -1);
    }

    /// <summary>
    /// Executes the update callbacks registered in the current state.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a parameter builder associated with this state machine has not been finalized.</exception>
    public void Update()
    {
        if (parameterBuilderFirstIndex != -1) ThrowHelper.ThrowInvalidOperationException_AParameterBuilderHasNotBeenFinalized();
        RunUpdate(currentState, default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnqueueAndRunIfNotRunning(TEvent @event, int parametersStartAt)
    {
        queue.StoreLast(new(@event, parametersStartAt));

        if (reservedForRun == -1)
            Slow();

        void Slow()
        {
            reservedForRun = queue.Last;
            try
            {
                while (queue.TryDequeue(out (TEvent Event, int ParameterIndex) event_))
                    Run(event_.Event, event_.ParameterIndex);
            }
            catch
            {
                ClearQueue();
                throw;
            }
            finally
            {
                reservedForRun = -1;
            }
        }
    }

    private void EnqueueAndRun(TEvent @event, int parametersStartAt)
    {
        int start = reservedForRun;
        reservedForRun = queue.StoreLast(new(@event, parametersStartAt), false);

        if (start == -1)
        {
            try
            {
                while (queue.TryDequeue(out (TEvent Event, int ParameterIndex) event_))
                    Run(event_.Event, event_.ParameterIndex);
            }
            catch
            {
                ClearQueue();
                throw;
            }
            finally
            {
                reservedForRun = -1;
            }
        }
        else
        {
            SlotsQueue<(TEvent Event, int ParameterIndex)>.Enumerator enumerator = queue.GetEnumeratorStartingAt(start);
            Debug.Assert(enumerator.Has);
            try
            {
                do
                {
                    (TEvent Event, int ParameterIndex) event_ = enumerator.Current;
                    queue.Remove(enumerator.CurrentIndex);

                    Run(event_.Event, event_.ParameterIndex);
                } while (enumerator.Next());
            }
            catch
            {
                Catch(enumerator);
                throw;
            }
            finally
            {
                reservedForRun = start;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void Catch(SlotsQueue<(TEvent Event, int ParameterIndex)>.Enumerator enumerator)
        {
            do
            {
                RemoveParameters(enumerator.Current.ParameterIndex);
            } while (enumerator.Next());
        }
    }

    private void Run(TEvent @event, int parameterIndex)
    {
        SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator = parameterIndex == -1 ? default : parameterIndexes.GetEnumeratorStartingAt(parameterIndex);
        try
        {
            int currentState = this.currentState;
            if (!flyweight.TransitionStartIndexes.TryGetValue((currentState, @event), out int transitionIndex))
                ThrowHelper.ThrowInvalidOperationException_EventNotRegisterForState(flyweight.States[currentState].state, @event);

#if NET5_0_OR_GREATER
            ref TransitionEventUnion transitionEvent_0 = ref MemoryMarshal.GetArrayDataReference(flyweight.TransitionEvents);
#else
            ref TransitionEventUnion transitionEvent_0 = ref flyweight.TransitionEvents[0];
#endif
            TRecipient recipient = this.recipient;
            while (true)
            {
                Debug.Assert(flyweight.TransitionEvents.Length >= transitionIndex);
                (TransitionResult Result, int Index) value = Unsafe.Add(ref transitionEvent_0, transitionIndex).Invoke(recipient, parametersEnumerator);
                switch (value.Result)
                {
                    case TransitionResult.Continue:
                        transitionIndex++;
                        continue;
                    case TransitionResult.Branch:
                        transitionIndex = value.Index;
                        continue;
                    case TransitionResult.GoTo:
                        this.currentState = value.Index;
                        goto outside;
                    case TransitionResult.StaySelf:
                        goto outside;
                }
            }
        outside:;
        }
        finally
        {
            if (parametersEnumerator.Has)
                parameterIndexes.RemoveFrom(parametersEnumerator.CurrentIndex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(int parametersStartIndex)
    {
        SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator = parameterIndexes.GetEnumeratorStartingAt(parametersStartIndex);
        try
        {
            RunUpdate(currentState, parametersEnumerator);
        }
        finally
        {
            Debug.Assert(parametersEnumerator.Has);
            parameterIndexes.RemoveFrom(parametersEnumerator.CurrentIndex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RunUpdate(int stateIndex, SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator)
    {
        State<TState> state = flyweight.States[stateIndex];
        if (state.TryGetParentState(out int parentState))
            RunUpdate(parentState, parametersEnumerator);
        if (state.onUpdateLength != 0)
        {
            TRecipient recipient = this.recipient;
#if NET5_0_OR_GREATER
            ref StateEventUnion current = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(flyweight.StateEvents), state.onUpdateStart);
#else
            ref StateEventUnion current = ref flyweight.StateEvents[state.onUpdateStart];
#endif
            ref StateEventUnion end = ref Unsafe.Add(ref current, state.onUpdateLength);
            while (Unsafe.IsAddressLessThan(ref current, ref end))
            {
                current.Invoke(recipient, parametersEnumerator);
                current = ref Unsafe.Add(ref current, 1);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void StoreFirstParameterInBuilder<TParameter>(TParameter parameter)
    {
        if (!parameters.TryGetValue(typeof(TParameter), out ParameterSlots? container))
            container = CreateParameterSlot<TParameter>();
        Debug.Assert(container is ParameterSlots<TParameter>);
        int index = Unsafe.As<ParameterSlots<TParameter>>(container).Store(parameter, false);
        Debug.Assert(parameterBuilderFirstIndex == -1);
        parameterBuilderFirstIndex = parameterIndexes.StoreLast(new(container, index), false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)] // No inline to improve code quality since this is a cold path.
    private ParameterSlots<TParameter> CreateParameterSlot<TParameter>()
    {
        ParameterSlots<TParameter> container = new();
        parameters.Add(typeof(TParameter), container);
        return container;
    }

    [MethodImpl(MethodImplOptions.NoInlining)] // No inline to improve code quality since this is a cold path.
    private void ClearQueue()
    {
        while (queue.TryDequeue(out (TEvent Event, int ParameterIndex) event_))
        {
            if (event_.ParameterIndex != -1)
                RemoveParameters(event_.ParameterIndex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveParameters(int parameterIndex)
    {
        if (parameterIndex == -1)
            return;
        parameterIndexes.RemoveFrom(parameterIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Initialize(TRecipient recipient)
    {
        parameterBuilderVersion++;
        this.recipient = recipient;
        int index = parameterBuilderFirstIndex;
        Debug.Assert(index != -1);
        parameterBuilderFirstIndex = -1;
        SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator = parameterIndexes.GetEnumeratorStartingAt(index);
        StateEventUnion[] stateEvents = flyweight.StateEvents;
        for (int i = flyweight.InitialStateOnEntryStart; i < stateEvents.Length; i++)
            stateEvents[i].Invoke(recipient, parametersEnumerator);
        parameterIndexes.RemoveFrom(index);
    }
}
