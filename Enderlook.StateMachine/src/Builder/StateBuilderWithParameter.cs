using System;

namespace Enderlook.StateMachine;

/// <summary>
/// Wrapper around <see cref="StateBuilder{TState, TEvent, TRecipient}"/> for subscription of delegates with parameters.
/// </summary>
/// <typeparam name="TState">Type that determines states.</typeparam>
/// <typeparam name="TEvent">Type that determines events.</typeparam>
/// <typeparam name="TRecipient">Type that determines internal data that can be acceded by actions.</typeparam>
/// <typeparam name="TParameter">Type of parameter passed to the action when a trigger is fired.</typeparam>
public readonly struct StateBuilderWithParameter<TState, TEvent, TRecipient, TParameter>
    where TState : notnull
    where TEvent : notnull
{
    private readonly StateBuilder<TState, TEvent, TRecipient> owner;

    internal StateBuilderWithParameter(StateBuilder<TState, TEvent, TRecipient> owner) => this.owner = owner;

    /// <summary>
    /// Determines an action to execute on entry to this state.<br/>
    /// This action will only be executed if the event is fired with the specific <typeparamref name="TParameter"/> type.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnEntry(Action<TParameter> action)
    {
        if (owner.Upgrade is not null) return owner.Upgrade.WithParameter<TParameter>().OnEntry(action);
        if (owner.parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (owner.onEntry ??= new()).Add(new(action, StateEventType.HasParameter));
        return owner;
    }

    /// <summary>
    /// Determines an action to execute on entry to this state.<br/>
    /// This action will only be executed if the event is fired with the specific <typeparamref name="TParameter"/> type.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnEntry(Action<TRecipient, TParameter> action)
    {
        if (owner.Upgrade is not null) return owner.Upgrade.WithParameter<TParameter>().OnEntry(action);
        if (owner.parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (owner.onEntry ??= new()).Add(new(action, StateEventType.HasRecipient | StateEventType.HasParameter));
        return owner;
    }

    /// <summary>
    /// Determines an action to execute on exit to this state.<br/>
    /// This action will only be executed if the event is fired with the specific <typeparamref name="TParameter"/> type.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnExit(Action<TParameter> action)
    {
        if (owner.Upgrade is not null) return owner.Upgrade.WithParameter<TParameter>().OnEntry(action);
        if (owner.parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (owner.onExit ??= new()).Add(new(action, StateEventType.HasParameter));
        return owner;
    }

    /// <summary>
    /// Determines an action to execute on exit to this state.<br/>
    /// This action will only be executed if the event is fired with the specific <typeparamref name="TParameter"/> type.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnExit(Action<TRecipient, TParameter> action)
    {
        if (owner.Upgrade is not null) return owner.Upgrade.WithParameter<TParameter>().OnExit(action);
        if (owner.parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (owner.onExit ??= new()).Add(new(action, StateEventType.HasRecipient | StateEventType.HasParameter));
        return owner;
    }

    /// <summary>
    /// Determines an action to execute on update while in this state.<br/>
    /// This action will only be executed if the update is fired with the specific <typeparamref name="TParameter"/> type.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnUpdate(Action<TParameter> action)
    {
        if (owner.Upgrade is not null) return owner.Upgrade.WithParameter<TParameter>().OnUpdate(action);
        if (owner.parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (owner.onUpdate ??= new()).Add(new(action, StateEventType.HasParameter));
        return owner;
    }

    /// <summary>
    /// Determines an action to execute on update while in this state.<br/>
    /// This action will only be executed if the update is fired with the specific <typeparamref name="TParameter"/> type.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public StateBuilder<TState, TEvent, TRecipient> OnUpdate(Action<TRecipient, TParameter> action)
    {
        if (owner.Upgrade is not null) return owner.Upgrade.WithParameter<TParameter>().OnUpdate(action);
        if (owner.parent.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        (owner.onUpdate ??= new()).Add(new(action, StateEventType.HasRecipient | StateEventType.HasParameter));
        return owner;
    }
}