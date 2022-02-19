using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Enderlook.StateMachine;

/// <summary>
/// Builder of an state machine.
/// </summary>
/// <typeparam name="TState">Type that determines states.</typeparam>
/// <typeparam name="TEvent">Type that determines events.</typeparam>
/// <typeparam name="TRecipient">Type that determines internal data that can be acceded by actions.</typeparam>
public sealed class StateMachineBuilder<TState, TEvent, TRecipient> : IFinalizable
    where TState : notnull
    where TEvent : notnull
{
    private readonly Dictionary<TState, StateBuilder<TState, TEvent, TRecipient>> states = new();
    private bool hasInitialState;
    private TState? initialState;
    private bool runEntryActionsOfInitialState;

    bool IFinalizable.HasFinalized => HasFinalized;

    internal bool HasFinalized { get; private set; }

    /// <summary>
    /// Determines the initial state of the state machine.
    /// </summary>
    /// <param name="state">Initial state.</param>
    /// <param name="runEntryActions">If <see langword="true"/>, the actions stored in <see cref="StateBuilder{TState, TEvent, TRecipient}.OnEntry(Action)"/> (and overloads) of the state <see cref="State{TState}"/> will be run during intialization.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="state"/> is <see langword="null"/>.<br/>
    /// Thrown when the initial state was already registered.</exception>
    public StateMachineBuilder<TState, TEvent, TRecipient> SetInitialState(TState state, bool runEntryActions = true)
    {
        if (HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (state is null) ThrowHelper.ThrowArgumentNullException_State();
        if (hasInitialState) ThrowHelper.ThrowInvalidOperationException_AlreadyHasInitialState();
        hasInitialState = true;
        initialState = state;
        runEntryActionsOfInitialState = runEntryActions;
        return this;
    }

    /// <summary>
    /// Add a new state or loads a previously added state.
    /// </summary>
    /// <param name="state">State to add.</param>
    /// <returns>State builder.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="state"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> In(TState state)
    {
        if (HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (state is null) ThrowHelper.ThrowArgumentNullException_State();
        if (!states.TryGetValue(state, out StateBuilder<TState, TEvent, TRecipient>? builder))
        {
            builder = new StateBuilder<TState, TEvent, TRecipient>(this, state);
            states.Add(state, builder);
        }
        return builder;
    }

    /// <summary>
    /// Creates a factory of the current state machine plan.
    /// </summary>
    /// <returns>Created factory.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <see cref="SetInitialState(TState, bool)"/> was not called.<br/>
    /// Thrown when <see cref="In(TState)"/> was not called.<br/>
    /// Thrown when a transition was not terminated with <see cref="TransitionBuilder{TState, TEvent, TRecipient, TParent}.Goto(TState)"/>, <see cref="TransitionBuilder{TState, TEvent, TRecipient, TParent}.StaySelf"/> nor <see cref="TransitionBuilder{TState, TEvent, TRecipient, TParent}"/>.<br/>
    /// Thrown when an state has a not registered parent state passed to <see cref="StateBuilder{TState, TEvent, TRecipient}.IsSubStateOf(TState)"/>.
    /// </exception>
    public StateMachineFactory<TState, TEvent, TRecipient> Finalize()
    {
        if (HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        HasFinalized = true;
        if (!hasInitialState) ThrowHelper.ThrowInvalidOperationException_DoesNotHaveInitialState();
        Debug.Assert(initialState is not null);
        int statesCount = this.states.Count;
        if (statesCount == 0) ThrowHelper.ThrowInvalidOperationException_DoesNotHaveRegisteredStates();

        Dictionary<TState, int> statesMap = new(statesCount);
        int i = 0;
        int transitionEventsCount = 0;
        int stateEventsCount = 0;
        int transitionsCount = 0;
        foreach (KeyValuePair<TState, StateBuilder<TState, TEvent, TRecipient>> kv in this.states)
            kv.Value.PrepareAndCheck(statesMap, ref i, ref transitionEventsCount, ref stateEventsCount, ref transitionsCount);

        State<TState>[] states = new State<TState>[statesCount];
        StateEventUnion[] stateEvents = new StateEventUnion[stateEventsCount];
        TransitionEventUnion[] transitionEvents = new TransitionEventUnion[transitionEventsCount];
        Dictionary<(int State, TEvent Event), int> transitionStartIndexes = new(transitionsCount);

        int iStates = 0;
        int iStateEvents = 0;
        int iTransitionEvents = 0;
        // Don't use .Value because it allocates more memory.
        foreach (KeyValuePair<TState, StateBuilder<TState, TEvent, TRecipient>> kv in this.states)
            kv.Value.Save(statesMap, states, stateEvents, transitionEvents, transitionStartIndexes, ref iStates, ref iStateEvents, ref iTransitionEvents);

        Debug.Assert(transitionStartIndexes.Count == transitionsCount);

        return new StateMachineFactory<TState, TEvent, TRecipient>(states, stateEvents, transitionEvents, transitionStartIndexes, statesMap[initialState], runEntryActionsOfInitialState);
    }
}
