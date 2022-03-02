using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

/// <summary>
/// Builder of goto transitions.
/// </summary>
/// <typeparam name="TState">Type that determines states.</typeparam>
/// <typeparam name="TEvent">Type that determines events.</typeparam>
/// <typeparam name="TRecipient">Type that determines internal data that can be acceded by actions.</typeparam>
/// <typeparam name="TParent">Type of parent of the <see cref="TransitionBuilder{TState, TEvent, TRecipient, TParent}"/> instance which creates this instance.</typeparam>
public sealed class GotoBuilder<TState, TEvent, TRecipient, TParent> : IGoto<TState>
    where TState : notnull
    where TEvent : notnull
{
    private readonly TransitionBuilder<TState, TEvent, TRecipient, TParent> parent;
    // 0: Does not have state.
    // 1: Has state.
    // 2: Has GotoSelf state.
    private int hasState;
    private TState? state;
    private TransitionPolicy? onEntryPolicy;
    private TransitionPolicy? onExitPolicy;

    TState? IGoto<TState>.State => state;

    TransitionPolicy IGoto<TState>.OnEntryPolicy => onEntryPolicy ?? TransitionPolicy.ParentFirstWithCulling;

    TransitionPolicy IGoto<TState>.OnExitPolicy => onExitPolicy ?? TransitionPolicy.ChildFirstWithCulling;

    internal GotoBuilder(TransitionBuilder<TState, TEvent, TRecipient, TParent> parent) => this.parent = parent;

    /// <summary>
    /// Configures the policy of how subscribed delegates to on entry hook should be executed.<br/>
    /// If this method is not executed, the default policy is <see cref="TransitionPolicy.ParentFirstWithCulling"/>.
    /// </summary>
    /// <param name="policy">Policy of subscribed delegates execution.</param>
    /// <returns><see langword="this"/>.</returns>
    public GotoBuilder<TState, TEvent, TRecipient, TParent> OnEntryPolicy(TransitionPolicy policy)
    {
        Debug.Assert(parent is IStateMachineBuilderReacher<TState, TEvent, TRecipient>);
        if (Unsafe.As<IStateMachineBuilderReacher<TState, TEvent, TRecipient>>(parent).StateMachineBuilder.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (onEntryPolicy.HasValue) ThrowHelper.ThrowInvalidOperationException_AlreadyHasPolicy();
        onEntryPolicy = policy;
        return this;
    }

    /// <summary>
    /// Configures the policy of how subscribed delegates to on exit hook should be executed.<br/>
    /// If this method is not executed, the default policy is <see cref="TransitionPolicy.ChildFirstWithCulling"/>.
    /// </summary>
    /// <param name="policy">Policy of subscribed delegates execution.</param>
    /// <returns><see langword="this"/>.</returns>
    public GotoBuilder<TState, TEvent, TRecipient, TParent> OnExitPolicy(TransitionPolicy policy)
    {
        Debug.Assert(parent is IStateMachineBuilderReacher<TState, TEvent, TRecipient>);
        if (Unsafe.As<IStateMachineBuilderReacher<TState, TEvent, TRecipient>>(parent).StateMachineBuilder.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (onExitPolicy.HasValue) ThrowHelper.ThrowInvalidOperationException_AlreadyHasPolicy();
        onExitPolicy = policy;
        return this;
    }

    /// <summary>
    /// Determines to which state this transition goes.
    /// </summary>
    /// <param name="state">State to move</param>
    /// <returns>Creator of the instance which created this instance.</returns>
    public TParent Goto(TState state)
    {
        Debug.Assert(parent is IStateMachineBuilderReacher<TState, TEvent, TRecipient>);
        if (Unsafe.As<IStateMachineBuilderReacher<TState, TEvent, TRecipient>>(parent).StateMachineBuilder.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (hasState != 0) ThrowHelper.ThrowInvalidOperationException_AlreadyHasGoto();
        hasState = 1;
        this.state = state;
        return parent.Parent;
    }

    /// <summary>
    /// Determines to transite to the current state.
    /// </summary>
    /// <returns>Creator of the instance which created this instance.</returns>
    public TParent GotoSelf()
    {
        Debug.Assert(parent is IStateMachineBuilderReacher<TState, TEvent, TRecipient>);
        if (Unsafe.As<IStateMachineBuilderReacher<TState, TEvent, TRecipient>>(parent).StateMachineBuilder.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (hasState != 0) ThrowHelper.ThrowInvalidOperationException_AlreadyHasGoto();
        hasState = 2;
        return parent.Parent;
    }
}
