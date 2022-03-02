using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
        if (flyweight.RunEntryActionsOfInitialState)
            stateMachine.RunEntry(flyweight.InitialState, default);
        return stateMachine;
    }

    internal static StateMachine<TState, TEvent, TRecipient> FromWithParameter<TParameter>(StateMachineFactory<TState, TEvent, TRecipient> flyweight, TRecipient recipient, TParameter parameter)
    {
        StateMachine<TState, TEvent, TRecipient> stateMachine = new(flyweight, recipient);
        if (flyweight.RunEntryActionsOfInitialState)
            stateMachine.RunEntryAndDisposeParameters(flyweight.InitialState, stateMachine.parameterIndexes.GetEnumeratorStartingAt(stateMachine.StoreFirstParameter(parameter)));
        return stateMachine;
    }

    /// <summary>
    /// Returns the current (possibly sub) state of this state machine.
    /// </summary>
    public TState CurrentState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => flyweight.States[currentState].state;
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
            Debug.Assert(EqualityComparer<TState>.Default.Equals(CurrentState, currentStateHierarchy[0]));
            for (int i = 1; i < currentStateHierarchy.Count; i++)
            {
                TState state_ = currentStateHierarchy[i];
                if (EqualityComparer<TState>.Default.Equals(state_, state))
                    return true;
            }
        }
        else
        {
            EqualityComparer<TState> equalityComparer = EqualityComparer<TState>.Default;

            // Look for the first using CurrentState to avoid possible initialization of lazy.
            if (equalityComparer.Equals(CurrentState, state))
                return true;

            ReadOnlySlice<TState> currentStateHierarchy = CurrentStateHierarchy;
            Debug.Assert(equalityComparer.Equals(CurrentState, currentStateHierarchy[0]));
            for (int i = 1; i < currentStateHierarchy.Count; i++)
            {
                TState state_ = currentStateHierarchy[i];
                if (equalityComparer.Equals(state_, state))
                    return true;
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
    /// <exception cref="InvalidOperationException">Thrown a parameter builder associated with this state machine has not been finalized.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fire(TEvent @event)
    {
        if (@event is null) ThrowHelper.ThrowArgumentNullException_Event();
        if (parameterBuilderFirstIndex != -1) ThrowHelper.ThrowInvalidOperationException_AParameterBuilderHasNotBeenFinalized();
        EnqueueAndRunIfNotRunning(@event, -1);
    }

    /// <summary>
    /// Fire an event to the state machine.<br/>
    /// If the state machine is already firing an state, it's enqueued to run after completion of the current event.
    /// </summary>
    /// <typeparam name="TParameter">Type of parameter.</typeparam>
    /// <param name="event">Event to fire.</param>
    /// <param name="parameter">Parameter that can be passed to callbacks.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="event"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown a parameter builder associated with this state machine has not been finalized.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FireWithParameter<TParameter>(TEvent @event, TParameter parameter)
    {
        if (@event is null) ThrowHelper.ThrowArgumentNullException_Event();
        if (parameterBuilderFirstIndex != -1) ThrowHelper.ThrowInvalidOperationException_AParameterBuilderHasNotBeenFinalized();
        EnqueueAndRunIfNotRunning(@event, StoreFirstParameter(parameter));
    }

    /// <summary>
    /// Fire an event to the state machine.<br/>
    /// The event won't be enqueued but actually run, ignoring previously enqueued events.<br/>
    /// If subsequent events are enqueued during the execution of the callbacks of this event, they will also be run after the completion of this event.
    /// </summary>
    /// <param name="event">Event to fire.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="event"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown a parameter builder associated with this state machine has not been finalized.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FireImmediately(TEvent @event)
    {
        if (@event is null) ThrowHelper.ThrowArgumentNullException_Event();
        if (parameterBuilderFirstIndex != -1) ThrowHelper.ThrowInvalidOperationException_AParameterBuilderHasNotBeenFinalized();
        EnqueueAndRun(@event, -1);
    }

    /// <summary>
    /// Fire an event to the state machine.<br/>
    /// The event won't be enqueued but actually run, ignoring previously enqueued events.<br/>
    /// If subsequent events are enqueued during the execution of the callbacks of this event, they will also be run after the completion of this event.
    /// </summary>
    /// <typeparam name="TParameter">Type of parameter.</typeparam>
    /// <param name="event">Event to fire.</param>
    /// <param name="parameter">Parameter that can be passed to callbacks.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="event"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown a parameter builder associated with this state machine has not been finalized.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FireImmediatelyWithParameter<TParameter>(TEvent @event, TParameter parameter)
    {
        if (@event is null) ThrowHelper.ThrowArgumentNullException_Event();
        if (parameterBuilderFirstIndex != -1) ThrowHelper.ThrowInvalidOperationException_AParameterBuilderHasNotBeenFinalized();
        EnqueueAndRun(@event, StoreFirstParameter(parameter));
    }

    /// <summary>
    /// Executes the update callbacks registered in the current state.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown a parameter builder associated with this state machine has not been finalized.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        if (parameterBuilderFirstIndex != -1) ThrowHelper.ThrowInvalidOperationException_AParameterBuilderHasNotBeenFinalized();
        RunUpdate(currentState, default);
    }

    /// <summary>
    /// Executes the update callbacks registered in the current state.
    /// </summary>
    /// <typeparam name="TParameter">Type of parameter.</typeparam>
    /// <param name="parameter">Parameter that can be passed to callbacks.</param>
    /// <exception cref="InvalidOperationException">Thrown a parameter builder associated with this state machine has not been finalized.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateWithParameter<TParameter>(TParameter parameter)
    {
        if (parameterBuilderFirstIndex != -1) ThrowHelper.ThrowInvalidOperationException_AParameterBuilderHasNotBeenFinalized();
        int parametersStartIndex = StoreFirstParameter(parameter);
        Update(parametersStartIndex);
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
            State<TState>[] states = flyweight.States;
            if (!flyweight.TransitionStartIndexes.TryGetValue((currentState, @event), out int transitionIndex))
                ThrowHelper.ThrowInvalidOperationException_EventNotRegisterForState(states[currentState], @event);

            TransitionEventUnion[] transitionEvents = flyweight.TransitionEvents;
            StateEventUnion[] stateEvents = flyweight.StateEvents;
            TRecipient recipient = this.recipient;
            while (true)
            {
                TransitionEventUnion transitionEventUnion = transitionEvents[transitionIndex];
                (TransitionResult Result, int Index) value = transitionEventUnion.Invoke(recipient, parametersEnumerator);
                switch (value.Result)
                {
                    case TransitionResult.Continue:
                        transitionIndex++;
                        continue;
                    case TransitionResult.Branch:
                        transitionIndex = value.Index;
                        continue;
                    case TransitionResult.GoTo:
                        int stateIndex = currentState;
                        State<TState> state;
                        do
                        {
                            state = states[stateIndex];
                            if (state.onExitLength != -1)
                            {
                                int index = state.OnExitStart;
                                int to = index + state.onExitLength;
                                for (; index < to; index++)
                                    stateEvents[index].Invoke(recipient, parametersEnumerator);
                            }
                        } while (state.TryGetParentState(out stateIndex));

                        this.currentState = value.Index;

                        RunEntry(value.Index, parametersEnumerator);
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
    private void RunEntryAndDisposeParameters(int currentState, SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator)
    {
        RunEntry(currentState, parametersEnumerator);
        Debug.Assert(parametersEnumerator.Has);
        parameterIndexes.RemoveFrom(parametersEnumerator.CurrentIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RunEntry(int stateIndex, SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator)
    {
        State<TState> state = flyweight.States[stateIndex];
        if (state.TryGetParentState(out int parentState))
            RunEntry(parentState, parametersEnumerator);
        if (state.onEntryLength != 0)
        {
            int index = state.OnEntryStart;
            int to = index + state.onEntryLength;
            StateEventUnion[] stateEvents = flyweight.StateEvents;
            TRecipient recipient = this.recipient;
            for (; index < to; index++)
                stateEvents[index].Invoke(recipient, parametersEnumerator);
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
            int index = state.OnUpdateStart;
            int to = index + state.onUpdateLength;
            StateEventUnion[] stateEvents = flyweight.StateEvents;
            TRecipient recipient = this.recipient;
            for (; index < to; index++)
                stateEvents[index].Invoke(recipient, parametersEnumerator);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int StoreFirstParameter<TParameter>(TParameter parameter)
    {
        if (!parameters.TryGetValue(typeof(TParameter), out ParameterSlots? container))
            parameters.Add(typeof(TParameter), container = new ParameterSlots<TParameter>());
        Debug.Assert(container is ParameterSlots<TParameter>);
        int index = Unsafe.As<ParameterSlots<TParameter>>(container).Store(parameter, false);
        return parameterIndexes.StoreLast(new(container, index), false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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
        this.recipient = recipient;
        int index = parameterBuilderFirstIndex;
        parameterBuilderFirstIndex = -1;
        if (flyweight.RunEntryActionsOfInitialState)
            RunEntryAndDisposeParameters(currentState, parameterIndexes.GetEnumeratorStartingAt(index));
        else
            RemoveParameters(index);
    }
}