using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

/// <summary>
/// Builder of a concrete state.
/// </summary>
/// <typeparam name="TState">Type that determines states.</typeparam>
/// <typeparam name="TEvent">Type that determines events.</typeparam>
/// <typeparam name="TRecipient">Type that determines internal data that can be acceded by actions.</typeparam>
public sealed class StateBuilder<TState, TEvent, TRecipient> : IFinalizable
    where TState : notnull
    where TEvent : notnull
{
    private readonly StateMachineBuilder<TState, TEvent, TRecipient> parent;
    private readonly TState state;
    private readonly Dictionary<TEvent, TransitionBuilder<TState, TEvent, TRecipient, StateBuilder<TState, TEvent, TRecipient>>> transitions = new();
    private List<StateEventUnion>? onUpdate;
    private List<StateEventUnion>? onEntry;
    private List<StateEventUnion>? onExit;
    private TState? subStateOf;
    private bool isSubState;

    bool IFinalizable.HasFinalized
    {
        get
        {
            Debug.Assert(parent is IFinalizable);
            return Unsafe.As<IFinalizable>(parent).HasFinalized;
        }
    }

    internal StateBuilder(StateMachineBuilder<TState, TEvent, TRecipient> parent, TState state)
    {
        this.parent = parent;
        this.state = state;
    }

    /// <inheritdoc cref="StateBuilder{TState, TEvent, TRecipient}.In(TState)"/>
    public StateBuilder<TState, TEvent, TRecipient> In(TState state)
        => parent.In(state);

    /// <inheritdoc cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/>
    public StateMachineFactory<TState, TEvent, TRecipient> Finalize()
        => parent.Finalize();

    /// <summary>
    /// Marks this state as a substate of <paramref name="state"/>.
    /// </summary>
    /// <param name="state">Parent state of this state.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="state"/> is <see langword="null"/><br/>
    /// Thrown when <paramref name="state"/> is the current state.</exception>
    public StateBuilder<TState, TEvent, TRecipient> IsSubStateOf(TState state)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (state is null) ThrowHelper.ThrowArgumentNullException_State();
        if (isSubState) ThrowHelper.ThrowArgumentException_AlreadyIsSubState();
        if (EqualityComparer<TState>.Default.Equals(state, this.state)) ThrowHelper.ThrowArgumentException_StateCanNotBeSubStateOfItself();
        isSubState = true;
        subStateOf = state;
        return this;
    }

    /// <summary>
    /// Determines an action to execute on entry to this state.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnEntry(Action action)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (onEntry ??= new()).Add(new(action, StateEventType.Empty));
        return this;
    }

    /// <summary>
    /// Determines an action to execute on entry to this state.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnEntry(Action<TRecipient> action)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (onEntry ??= new()).Add(new(action, StateEventType.HasRecipient));
        return this;
    }

    /// <summary>
    /// Determines an action to execute on entry to this state.<br/>
    /// This action will only be executed if the event is fired with the specific <typeparamref name="TParameter"/> type.
    /// </summary>
    /// <typeparam name="TParameter">Type of parameter passed to the action when a trigger is fired.</typeparam>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnEntry<TParameter>(Action<TParameter> action)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (onEntry ??= new()).Add(new(action, StateEventType.HasParameter));
        return this;
    }

    /// <summary>
    /// Determines an action to execute on entry to this state.<br/>
    /// This action will only be executed if the event is fired with the specific <typeparamref name="TParameter"/> type.
    /// </summary>
    /// <typeparam name="TParameter">Type of parameter passed to the action when a trigger is fired.</typeparam>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnEntry<TParameter>(Action<TRecipient, TParameter> action)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (onExit ??= new()).Add(new(action, StateEventType.HasRecipient | StateEventType.HasParameter));
        return this;
    }

    /// <summary>
    /// Determines an action to execute on entry to this state.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnExit(Action action)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (onExit ??= new()).Add(new(action, StateEventType.Empty));
        return this;
    }

    /// <summary>
    /// Determines an action to execute on exit to this state.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnExit(Action<TRecipient> action)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (onExit ??= new()).Add(new(action, StateEventType.HasRecipient));
        return this;
    }

    /// <summary>
    /// Determines an action to execute on exit to this state.<br/>
    /// This action will only be executed if the event is fired with the specific <typeparamref name="TParameter"/> type.
    /// </summary>
    /// <typeparam name="TParameter">Type of parameter passed to the action when a trigger is fired.</typeparam>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnExit<TParameter>(Action<TParameter> action)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (onExit ??= new()).Add(new(action, StateEventType.HasParameter));
        return this;
    }

    /// <summary>
    /// Determines an action to execute on exit to this state.<br/>
    /// This action will only be executed if the event is fired with the specific <typeparamref name="TParameter"/> type.
    /// </summary>
    /// <typeparam name="TParameter">Type of parameter passed to the action when a trigger is fired.</typeparam>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnExit<TParameter>(Action<TRecipient, TParameter> action)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (onExit ??= new()).Add(new(action, StateEventType.HasRecipient | StateEventType.HasParameter));
        return this;
    }

    /// <summary>
    /// Determines an action to execute on eupdate while in this state.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnUpdate(Action action)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (onUpdate ??= new()).Add(new(action, StateEventType.Empty));
        return this;
    }

    /// <summary>
    /// Determines an action to execute on update while in this state.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnUpdate(Action<TRecipient> action)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (onUpdate ??= new()).Add(new(action, StateEventType.HasRecipient));
        return this;
    }

    /// <summary>
    /// Determines an action to execute on update while in this state.<br/>
    /// This action will only be executed if the update is fired with the specific <typeparamref name="TParameter"/> type.
    /// </summary>
    /// <typeparam name="TParameter">Type of parameter passed to the action when a trigger is fired.</typeparam>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnUpdate<TParameter>(Action<TParameter> action)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (onUpdate ??= new()).Add(new(action, StateEventType.HasParameter));
        return this;
    }

    /// <summary>
    /// Determines an action to execute on update while in this state.<br/>
    /// This action will only be executed if the update is fired with the specific <typeparamref name="TParameter"/> type.
    /// </summary>
    /// <typeparam name="TParameter">Type of parameter passed to the action when a trigger is fired.</typeparam>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnUpdate<TParameter>(Action<TRecipient, TParameter> action)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (onUpdate ??= new()).Add(new(action, StateEventType.HasRecipient | StateEventType.HasParameter));
        return this;
    }

    /// <summary>
    /// Add a behaviour that is executed on an event.
    /// </summary>
    /// <param name="event">Raised event.</param>
    /// <returns>Transition builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="event"/> is <see langword="null"/>.</exception>
    public TransitionBuilder<TState, TEvent, TRecipient, StateBuilder<TState, TEvent, TRecipient>> On(TEvent @event)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (@event is null) ThrowHelper.ThrowArgumentNullException_Event();
        if (transitions.ContainsKey(@event)) ThrowHelper.ThrowArgumentException_AlreadyHasEvent();
        TransitionBuilder<TState, TEvent, TRecipient, StateBuilder<TState, TEvent, TRecipient>> builder = new(this);
        transitions.Add(@event, builder);
        return builder;
    }

    /// <summary>
    /// Ignore this event.<br/>
    /// Equivalent to <c>.On(@event).StaySelf()</c>.
    /// </summary>
    /// <param name="event">Event to ignore.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="event"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> Ignore(TEvent @event)
        => On(@event).StaySelf();

    internal bool IsSubState([NotNullWhen(true)] out TState? state)
    {
        if (isSubState)
        {
            state = subStateOf;
            Debug.Assert(state is not null);
            return true;
        }
#if NET5_0_OR_GREATER
        Unsafe.SkipInit(out state);
#else
        state = default;
#endif
        return false;
    }

    internal void PrepareAndCheck(Dictionary<TState, int> statesMap, ref int i, ref int transitionEventsCount, ref int stateEventsCount, ref int transitionsCount)
    {
        statesMap.Add(state, i++);
        stateEventsCount += (onEntry?.Count ?? 0) + (onExit?.Count ?? 0) + (onUpdate?.Count ?? 0);
        transitionsCount += transitions.Count;
        // Don't use .Value because it allocates more memory.
        foreach (KeyValuePair<TEvent, TransitionBuilder<TState, TEvent, TRecipient, StateBuilder<TState, TEvent, TRecipient>>> kv in transitions)
            transitionEventsCount += kv.Value.GetTotalTransitionsAndEnsureHasTerminator();
    }

    internal void Save(Dictionary<TState, int> statesMap, State<TState>[] states, StateEventUnion[] stateEvents, TransitionEventUnion[] transitionEvents, Dictionary<(int State, TEvent Event), int> transitionStartIndexes, ref int iStates, ref int iStateEvents, ref int iTransitionEvents)
    {
        int onEventsStart = iStateEvents;

        int onUpdateLength;
        if (onUpdate is List<StateEventUnion> onUpdate_)
        {
            onUpdateLength = onUpdate_.Count;
            onUpdate_.CopyTo(0, stateEvents, iStateEvents, onUpdateLength);
            iStateEvents += onUpdateLength;
        }
        else
            onUpdateLength = 0;

        int onEntryLength;
        if (onEntry is List<StateEventUnion> onEntry_)
        {
            onEntryLength = onEntry_.Count;
            onEntry_.CopyTo(0, stateEvents, iStateEvents, onEntryLength);
            iStateEvents += onEntryLength;
        }
        else
            onEntryLength = 0;

        int onExitLength;
        if (onExit is List<StateEventUnion> onExit_)
        {
            onExitLength = onExit_.Count;
            onExit_.CopyTo(0, stateEvents, iStateEvents, onExitLength);
            iStateEvents += onExitLength;
        }
        else
            onExitLength = 0;

        int iState = iStates;
        Debug.Assert(iState == statesMap[state]);
        if (isSubState)
        {
            Debug.Assert(subStateOf is not null);
            states[iStates++] = new State<TState>(state, statesMap[subStateOf], onEventsStart, onUpdateLength, onEntryLength, onExitLength);
        }
        else
            states[iStates++] = new State<TState>(state, -1, onEventsStart, onUpdateLength, onEntryLength, onExitLength);

        foreach (KeyValuePair<TEvent, TransitionBuilder<TState, TEvent, TRecipient, StateBuilder<TState, TEvent, TRecipient>>> transition in transitions)
        {
            transitionStartIndexes.Add((iState, transition.Key), iTransitionEvents);
            transition.Value.Save(statesMap, statesMap[state], transitionEvents, ref iTransitionEvents);
        }
    }
}
