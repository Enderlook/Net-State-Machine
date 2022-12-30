using System;

namespace Enderlook.StateMachine;

/// <summary>
/// Builder of a concrete state which also holds an internal data.
/// </summary>
/// <typeparam name="TState">Type that determines states.</typeparam>
/// <typeparam name="TEvent">Type that determines events.</typeparam>
/// <typeparam name="TRecipient">Type that determines internal data that can be acceded by actions.</typeparam>
/// <typeparam name="TStateRecipient">Type that determines internal data of this state that can be acceded by actions.</typeparam>
public sealed class StateBuilder<TState, TEvent, TRecipient, TStateRecipient> : StateBuilder<TState, TEvent, TRecipient>
    where TState : notnull
    where TEvent : notnull
{
    private Delegate? stateRecipientFactory;

    internal StateBuilder(StateMachineBuilder<TState, TEvent, TRecipient> parent, TState state, Func<TRecipient, TStateRecipient> factory) : base(parent, state)
    {
        stateRecipientFactory = factory;
    }

    internal StateBuilder(StateMachineBuilder<TState, TEvent, TRecipient> parent, TState state, Func<TStateRecipient> factory) : base(parent, state)
    {
        stateRecipientFactory = factory;
    }

    internal StateBuilder(StateMachineBuilder<TState, TEvent, TRecipient> parent, TState state) : base(parent, state) { }

    internal StateBuilder(StateBuilder<TState, TEvent, TRecipient> sibling, Func<TStateRecipient> factory) : base(sibling)
    {
        stateRecipientFactory = factory;
    }

    internal StateBuilder(StateBuilder<TState, TEvent, TRecipient> sibling, Func<TRecipient, TStateRecipient> factory) : base(sibling)
    {
        stateRecipientFactory = factory;
    }

    internal StateBuilder(StateBuilder<TState, TEvent, TRecipient> sibling) : base(sibling) { }

    internal bool TryInsertFactory(Func<TRecipient, TStateRecipient> factory)
    {
        if (stateRecipientFactory is not null)
            return false;
        stateRecipientFactory = factory;
        return true;
    }

    internal bool TryInsertFactory(Func<TStateRecipient> factory)
    {
        if (stateRecipientFactory is not null)
            return false;
        stateRecipientFactory = factory;
        return true;
    }

    /// <summary>
    /// Determines an action to execute on entry to this state.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnEntry(Action<TStateRecipient> action)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (onEntry ??= new()).Add(new(action, StateEventType.HasStateRecipient));
        return this;
    }

    /// <summary>
    /// Determines an action to execute on entry to this state.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnEntryWithRecipient(Action<TRecipient, TStateRecipient> action)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (onEntry ??= new()).Add(new(action, StateEventType.HasRecipient | StateEventType.HasStateRecipient));
        return this;
    }

    /// <summary>
    /// Determines an action to execute on exit to this state.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnExit(Action<TStateRecipient> action)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (onExit ??= new()).Add(new(action, StateEventType.HasStateRecipient));
        return this;
    }

    /// <summary>
    /// Determines an action to execute on exit to this state.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnExit(Action<TRecipient, TStateRecipient> action)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (onExit ??= new()).Add(new(action, StateEventType.HasRecipient | StateEventType.HasStateRecipient));
        return this;
    }

    /// <summary>
    /// Determines an action to execute on update while in this state.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnUpdate(Action<TStateRecipient> action)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (onUpdate ??= new()).Add(new(action, StateEventType.HasStateRecipient));
        return this;
    }

    /// <summary>
    /// Determines an action to execute on update while in this state.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnUpdate(Action<TRecipient, TStateRecipient> action)
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (onUpdate ??= new()).Add(new(action, StateEventType.HasRecipient | StateEventType.HasStateRecipient));
        return this;
    }

    /// <summary>
    /// Give access to delegates that uses a parameter.
    /// </summary>
    /// <typeparam name="TParameter">Type of parameter passed to the action when a trigger is fired.</typeparam>
    /// <returns>A wrapper of this instance that allow access to delegates that uses a parameter.</returns>
    public StateBuilderWithParameter<TState, TEvent, TRecipient, TStateRecipient, TParameter> WithParameter<TParameter>()
    {
        if (parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        return new(this);
    }
}