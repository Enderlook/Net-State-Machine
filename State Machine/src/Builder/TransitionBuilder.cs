using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

/// <summary>
/// Builder of concrete transitions.
/// </summary>
/// <typeparam name="TState">Type that determines states.</typeparam>
/// <typeparam name="TEvent">Type that determines events.</typeparam>
/// <typeparam name="TRecipient">Type that determines internal data that can be acceded by actions.</typeparam>
/// <typeparam name="TParent">Type of parent which creates this instance.</typeparam>
public sealed class TransitionBuilder<TState, TEvent, TRecipient, TParent> : IFinalizable, ITransitionBuilder<TState>
    where TState : notnull
    where TEvent : notnull
{
    private readonly TParent parent;
    private readonly List<TransitionBuilderUnion<TState, TEvent, TRecipient>> actions = new();

    internal TransitionBuilder(TParent parent) => this.parent = parent;

    bool IFinalizable.HasFinalized
    {
        get
        {
            Debug.Assert(parent is IFinalizable);
            return Unsafe.As<IFinalizable>(parent).HasFinalized;
        }
    }

    /// <summary>
    /// Add a sub transition with a condition.
    /// </summary>
    /// <param name="guard">Condition to execute transition.</param>
    /// <returns>Sub transition.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="guard"/> is <see langword="null"/>.</exception>
    public TransitionBuilder<TState, TEvent, TRecipient, TransitionBuilder<TState, TEvent, TRecipient, TParent>> If(Func<bool> guard)
    {
        Debug.Assert(parent is IFinalizable);
        if (Unsafe.As<IFinalizable>(parent).HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (guard is null) ThrowHelper.ThrowArgumentNullException_Guard();
        TransitionBuilder<TState, TEvent, TRecipient, TransitionBuilder<TState, TEvent, TRecipient, TParent>> branch = new(this);
        actions.Add(new(guard, TransitionEventBuilderType.IsBranch, branch, default));
        return branch;
    }

    /// <summary>
    /// Add a sub transition with a condition.
    /// </summary>
    /// <param name="guard">Condition to execute transition.</param>
    /// <returns>Sub transition.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="guard"/> is <see langword="null"/>.</exception>
    public TransitionBuilder<TState, TEvent, TRecipient, TransitionBuilder<TState, TEvent, TRecipient, TParent>> If(Func<TRecipient, bool> guard)
    {
        Debug.Assert(parent is IFinalizable);
        if (Unsafe.As<IFinalizable>(parent).HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (guard is null) ThrowHelper.ThrowArgumentNullException_Guard();
        TransitionBuilder<TState, TEvent, TRecipient, TransitionBuilder<TState, TEvent, TRecipient, TParent>> branch = new(this);
        actions.Add(new(guard, TransitionEventBuilderType.IsBranch | TransitionEventBuilderType.HasRecipient, branch, default));
        return branch;
    }

    /// <summary>
    /// Add a sub transition with a condition.<br/>
    /// This guard will only be executed if the event is fired with the specific <typeparamref name="TParameter"/> type.
    /// </summary>
    /// <param name="guard">Condition to execute transition.</param>
    /// <returns>Sub transition.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="guard"/> is <see langword="null"/>.</exception>
    public TransitionBuilder<TState, TEvent, TRecipient, TransitionBuilder<TState, TEvent, TRecipient, TParent>> If<TParameter>(Func<TParameter, bool> guard)
    {
        Debug.Assert(parent is IFinalizable);
        if (Unsafe.As<IFinalizable>(parent).HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (guard is null) ThrowHelper.ThrowArgumentNullException_Guard();
        TransitionBuilder<TState, TEvent, TRecipient, TransitionBuilder<TState, TEvent, TRecipient, TParent>> branch = new(this);
        actions.Add(new(guard, TransitionEventBuilderType.IsBranch | TransitionEventBuilderType.HasParameter, branch, default));
        return branch;
    }

    /// <summary>
    /// Add a sub transition with a condition.<br/>
    /// This guard will only be executed if the event is fired with the specific <typeparamref name="TParameter"/> type.
    /// </summary>
    /// <param name="guard">Condition to execute transition.</param>
    /// <returns>Sub transition.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="guard"/> is <see langword="null"/>.</exception>
    public TransitionBuilder<TState, TEvent, TRecipient, TransitionBuilder<TState, TEvent, TRecipient, TParent>> If<TParameter>(Func<TRecipient, TParameter, bool> guard)
    {
        Debug.Assert(parent is IFinalizable);
        if (Unsafe.As<IFinalizable>(parent).HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (guard is null) ThrowHelper.ThrowArgumentNullException_Guard();
        TransitionBuilder<TState, TEvent, TRecipient, TransitionBuilder<TState, TEvent, TRecipient, TParent>> branch = new(this);
        actions.Add(new(guard, TransitionEventBuilderType.IsBranch | TransitionEventBuilderType.HasParameter | TransitionEventBuilderType.HasRecipient, branch, default));
        return branch;
    }

    /// <summary>
    /// Determines an action to execute when the event is raised.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public TransitionBuilder<TState, TEvent, TRecipient, TParent> Do(Action action)
    {
        Debug.Assert(parent is IFinalizable);
        if (Unsafe.As<IFinalizable>(parent).HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        actions.Add(new(action, TransitionEventBuilderType.Empty, null, default));
        return this;
    }

    /// <summary>
    /// Determines an action to execute when the event is raised.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public TransitionBuilder<TState, TEvent, TRecipient, TParent> Do(Action<TRecipient> action)
    {
        Debug.Assert(parent is IFinalizable);
        if (Unsafe.As<IFinalizable>(parent).HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        actions.Add(new(action, TransitionEventBuilderType.HasRecipient, null, default));
        return this;
    }

    /// <summary>
    /// Determines an action to execute when the event is raised.<br/>
    /// This action will only be executed if the event is fired with the specific <typeparamref name="TParameter"/> type.
    /// </summary>
    /// <typeparam name="TParameter">Type of parameter passed to the action when a trigger is fired.</typeparam>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public TransitionBuilder<TState, TEvent, TRecipient, TParent> Do<TParameter>(Action<TParameter> action)
    {
        Debug.Assert(parent is IFinalizable);
        if (Unsafe.As<IFinalizable>(parent).HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        actions.Add(new(action, TransitionEventBuilderType.HasParameter, null, default));
        return this;
    }

    /// <summary>
    /// Determines an action to execute when the event is raised.<br/>
    /// This action will only be executed if the event is fired with the specific <typeparamref name="TParameter"/> type.
    /// </summary>
    /// <typeparam name="TParameter">Type of parameter passed to the action when a trigger is fired.</typeparam>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public TransitionBuilder<TState, TEvent, TRecipient, TParent> Do<TParameter>(Action<TRecipient, TParameter> action)
    {
        Debug.Assert(parent is IFinalizable);
        if (Unsafe.As<IFinalizable>(parent).HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        actions.Add(new(action, TransitionEventBuilderType.HasRecipient | TransitionEventBuilderType.HasParameter, null, default));
        return this;
    }

    /// <summary>
    /// Determines to which state this transition goes.
    /// </summary>
    /// <param name="state">State to move</param>
    /// <returns>Creator of this instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="state"/> is <see langword="null"/>.</exception>
    public TParent Goto(TState state)
    {
        Debug.Assert(parent is IFinalizable);
        if (Unsafe.As<IFinalizable>(parent).HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (state is null) ThrowHelper.ThrowArgumentNullException_State();
        actions.Add(new(null, TransitionEventBuilderType.IsGoTo, null, state));
        return parent;
    }

    /// <summary>
    /// Determines to transite to the current state.<br/>
    /// That means, that on exit and on entry actions of current state (but not parent states in case current state is a substate) will be executed.
    /// </summary>
    /// <returns>Creator of this instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.</exception>
    public TParent GotoSelf()
    {
        Debug.Assert(parent is IFinalizable);
        if (Unsafe.As<IFinalizable>(parent).HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        actions.Add(new(null, TransitionEventBuilderType.IsGoToSelf, null, default));
        return parent;
    }

    /// <summary>
    /// Determines that will have no transition to any state, so no on entry nor on exit event will be raised.
    /// </summary>
    /// <returns>Creator of this instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.</exception>
    public TParent StaySelf()
    {
        Debug.Assert(parent is IFinalizable);
        if (Unsafe.As<IFinalizable>(parent).HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        actions.Add(new(null, TransitionEventBuilderType.IsStaySelf, null, default));
        return parent;
    }

    internal int GetTotalTransitionsAndEnsureHasTerminator()
    {
        if (actions.Count == 0 || !actions[actions.Count - 1].IsTerminator) ThrowHelper.ThrowInvalidOperationException_TransitionMustHaveTerminator();
        int total = actions.Count;
        foreach (TransitionBuilderUnion<TState, TEvent, TRecipient> action in actions)
            total += action.GetTotalTransitionsAndEnsureHasTerminator();
        return total;
    }

    int ITransitionBuilder<TState>.GetTotalTransitionsAndEnsureHasTerminator()
        => GetTotalTransitionsAndEnsureHasTerminator();

    internal void Save(Dictionary<TState, int> statesMap, int currentState, TransitionEventUnion[] transitionEvents, ref int iTransitionEvents)
    {
        int i = iTransitionEvents;
        iTransitionEvents += actions.Count;
        foreach (TransitionBuilderUnion<TState, TEvent, TRecipient> action in actions)
            action.Save(statesMap, currentState, transitionEvents, ref i, ref iTransitionEvents);
    }

    void ITransitionBuilder<TState>.Save(Dictionary<TState, int> statesMap, int currentState, TransitionEventUnion[] transitionEvents, ref int iTransitionEvents)
        => Save(statesMap, currentState, transitionEvents, ref iTransitionEvents);
}
