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
public sealed class StateMachine<TState, TEvent, TRecipient>
    where TState : notnull
    where TEvent : notnull
{
    private readonly StateMachineFactory<TState, TEvent, TRecipient> flyweight;

    private readonly TRecipient recipient;

    private int currentState;

    private readonly Dictionary<Type, ParameterSlots> parameters = new();
    private SlotsQueue<ParameterSlot> parameterIndexes = new();
    private SlotsQueue<(TEvent Event, int ParameterIndex)> queue = new(1);
    private int reservedForRun = -1;

    internal StateMachine(StateMachineFactory<TState, TEvent, TRecipient> flyweight, TRecipient recipient, int currentState)
    {
        this.flyweight = flyweight;
        this.recipient = recipient;
        this.currentState = currentState;
    }

    internal static StateMachine<TState, TEvent, TRecipient> From(
        StateMachineFactory<TState, TEvent, TRecipient> flyweight,
        TRecipient recipient,
        int currentState)
    {
        StateMachine<TState, TEvent, TRecipient> stateMachine = new(flyweight, recipient, currentState);
        if (flyweight.RunEntryActionsOfInitialState)
            stateMachine.RunEntry(currentState, default);
        return stateMachine;
    }

    internal static StateMachine<TState, TEvent, TRecipient> From<TParameter>(
        StateMachineFactory<TState, TEvent, TRecipient> flyweight,
        TRecipient recipient,
        int currentState,
        TParameter parameter)
        where TParameter : IParameter
    {
        StateMachine<TState, TEvent, TRecipient> stateMachine = new(flyweight, recipient, currentState);
        if (flyweight.RunEntryActionsOfInitialState)
            stateMachine.RunEntryAndDisposeParameters(currentState, stateMachine.parameterIndexes.GetEnumeratorStartingAt(parameter.Store<Yes>(ref stateMachine.parameterIndexes, stateMachine.parameters)));
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
    /// <exception cref="ArgumentNullException">Throw when <paramref name="state"/> is <see langword="null"/>.</exception>
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
    /// <exception cref="ArgumentNullException">Throw when <paramref name="state"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySlice<TState> GetParentHierarchyOf(TState state)
    {
        if (state is null) ThrowHelper.ThrowArgumentNullException_State();
        return flyweight.GetParentHierarchyOfState(state);
    }

    /// <summary>
    /// Returns the events that are accepted by <paramref name="state"/>, if any.
    /// </summary>
    /// <param name="state">Staete whose accepted events are looked for.</param>
    /// <returns>Accepted events by <paramref name="state"/>. May be empty if <paramref name="state"/> doesn't accept any other event (is terminal).</returns>
    /// <exception cref="ArgumentNullException">Throw when <paramref name="state"/> is <see langword="null"/>.</exception>
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
    /// <exception cref="ArgumentNullException">Throw when <paramref name="state"/> is <see langword="null"/>.</exception>
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
    /// <exception cref="ArgumentNullException">Throw when <paramref name="event"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fire(TEvent @event)
    {
        if (@event is null) ThrowHelper.ThrowArgumentNullException_Event();
        EnqueueAndRunIfNotRunning(@event, -1);
    }

    /// <summary>
    /// Fire an event to the state machine.<br/>
    /// If the state machine is already firing an state, it's enqueued to run after completion of the current event.
    /// </summary>
    /// <typeparam name="TParameter">Type of parameter.</typeparam>
    /// <param name="event">Event to fire.</param>
    /// <param name="parameter">Parameter that can be passed to callbacks.</param>
    /// <exception cref="ArgumentNullException">Throw when <paramref name="event"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fire<TParameter>(TEvent @event, TParameter parameter)
        where TParameter : IParameter
    {
        if (@event is null) ThrowHelper.ThrowArgumentNullException_Event();
        EnqueueAndRunIfNotRunning(@event, parameter.Store<Yes>(ref parameterIndexes, parameters));
    }

    /// <summary>
    /// Fire an event to the state machine.<br/>
    /// The event won't be enqueued but actually run, ignoring previously enqueued events.<br/>
    /// If subsequent events are enqueued during the execution of the callbacks of this event, they will also be run after the completion of this event.
    /// </summary>
    /// <param name="event">Event to fire.</param>
    /// <exception cref="ArgumentNullException">Throw when <paramref name="event"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FireImmediately(TEvent @event)
    {
        if (@event is null) ThrowHelper.ThrowArgumentNullException_Event();
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
    /// <exception cref="ArgumentNullException">Throw when <paramref name="event"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FireImmediately<TParameter>(TEvent @event, TParameter parameter)
        where TParameter : IParameter
    {
        if (@event is null) ThrowHelper.ThrowArgumentNullException_Event();
        EnqueueAndRun(@event, parameter.Store<Yes>(ref parameterIndexes, parameters));
    }

    /// <summary>
    /// Executes the update callback registered in the current state.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update() => Update_(default);

    /// <summary>
    /// Executes the update callback registered in the current state.
    /// </summary>
    /// <typeparam name="TParameter">Type of parameter.</typeparam>
    /// <param name="parameter">Parameter that can be passed to callbacks.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update<TParameter>(TParameter parameter)
        where TParameter : IParameter
        => Update_(parameterIndexes.GetEnumeratorStartingAt(parameter.Store<Yes>(ref parameterIndexes, parameters)));

    private void Update_(SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator)
    {
        try
        {
            RunUpdate(currentState, parametersEnumerator);
        }
        finally
        {
            if (parametersEnumerator.Has)
            {
                do
                {
                    parameterIndexes.Remove(parametersEnumerator.CurrentIndex);
                    parametersEnumerator.Current.Remove();
                } while (parametersEnumerator.Next());
            }
        }
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
                queue.Clear();
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
                queue.Clear();
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
                queue.RemoveFrom(enumerator.CurrentIndex);
                throw;
            }
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
            {
                do
                {
                    parameterIndexes.Remove(parametersEnumerator.CurrentIndex);
                    parametersEnumerator.Current.Remove();
                } while (parametersEnumerator.Next());
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RunEntryAndDisposeParameters(int currentState, SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator)
    {
        RunEntry(currentState, parametersEnumerator);
        if (parametersEnumerator.Has)
        {
            do
            {
                parameterIndexes.Remove(parametersEnumerator.CurrentIndex);
                parametersEnumerator.Current.Remove();
            } while (parametersEnumerator.Next());
        }
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
            for (; index < to; index++)
                stateEvents[index].Invoke(recipient, parametersEnumerator);
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
            for (; index < to; index++)
                stateEvents[index].Invoke(recipient, parametersEnumerator);
        }
    }
}
