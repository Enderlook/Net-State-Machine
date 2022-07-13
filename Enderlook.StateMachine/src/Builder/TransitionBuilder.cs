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
public sealed class TransitionBuilder<TState, TEvent, TRecipient, TParent> : IStateMachineBuilderReacher<TState, TEvent, TRecipient>, ITransitionBuilder<TState, TEvent, TRecipient>
    where TState : notnull
    where TEvent : notnull
{
    internal readonly TParent Parent;
    private readonly List<TransitionBuilderUnion<TState, TEvent, TRecipient>> actions = new();

    internal TransitionBuilder(TParent parent) => this.Parent = parent;

    StateMachineBuilder<TState, TEvent, TRecipient> IStateMachineBuilderReacher<TState, TEvent, TRecipient>.StateMachineBuilder
    {
        get
        {
            Debug.Assert(Parent is IStateMachineBuilderReacher<TState, TEvent, TRecipient>);
            return Unsafe.As<IStateMachineBuilderReacher<TState, TEvent, TRecipient>>(Parent).StateMachineBuilder;
        }
    }

    internal StateBuilder<TState, TEvent, TRecipient> StateBuilder
    {
        get
        {
            if (Parent is StateBuilder<TState, TEvent, TRecipient> stateBuilder)
                return stateBuilder;
            Debug.Assert(Parent is ITransitionBuilder<TState, TEvent, TRecipient>);
            return Unsafe.As<ITransitionBuilder<TState, TEvent, TRecipient>>(Parent).StateBuilder;
        }
    }

    StateBuilder<TState, TEvent, TRecipient> ITransitionBuilder<TState, TEvent, TRecipient>.StateBuilder => StateBuilder;

    /// <summary>
    /// Add a sub transition with a condition.
    /// </summary>
    /// <param name="guard">Condition to execute transition.</param>
    /// <returns>Sub transition.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="guard"/> is <see langword="null"/>.</exception>
    public TransitionBuilder<TState, TEvent, TRecipient, TransitionBuilder<TState, TEvent, TRecipient, TParent>> If(Func<bool> guard)
    {
        Debug.Assert(Parent is IStateMachineBuilderReacher<TState, TEvent, TRecipient>);
        if (Unsafe.As<IStateMachineBuilderReacher<TState, TEvent, TRecipient>>(Parent).StateMachineBuilder.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (guard is null) ThrowHelper.ThrowArgumentNullException_Guard();
        TransitionBuilder<TState, TEvent, TRecipient, TransitionBuilder<TState, TEvent, TRecipient, TParent>> branch = new(this);
        actions.Add(new(guard, default, branch));
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
        Debug.Assert(Parent is IStateMachineBuilderReacher<TState, TEvent, TRecipient>);
        if (Unsafe.As<IStateMachineBuilderReacher<TState, TEvent, TRecipient>>(Parent).StateMachineBuilder.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (guard is null) ThrowHelper.ThrowArgumentNullException_Guard();
        TransitionBuilder<TState, TEvent, TRecipient, TransitionBuilder<TState, TEvent, TRecipient, TParent>> branch = new(this);
        actions.Add(new(guard, DelegateSignature.HasRecipient, branch));
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
        Debug.Assert(Parent is IStateMachineBuilderReacher<TState, TEvent, TRecipient>);
        if (Unsafe.As<IStateMachineBuilderReacher<TState, TEvent, TRecipient>>(Parent).StateMachineBuilder.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (guard is null) ThrowHelper.ThrowArgumentNullException_Guard();
        TransitionBuilder<TState, TEvent, TRecipient, TransitionBuilder<TState, TEvent, TRecipient, TParent>> branch = new(this);
        actions.Add(new(guard, DelegateSignature.HasParameter, branch));
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
        Debug.Assert(Parent is IStateMachineBuilderReacher<TState, TEvent, TRecipient>);
        if (Unsafe.As<IStateMachineBuilderReacher<TState, TEvent, TRecipient>>(Parent).StateMachineBuilder.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (guard is null) ThrowHelper.ThrowArgumentNullException_Guard();
        TransitionBuilder<TState, TEvent, TRecipient, TransitionBuilder<TState, TEvent, TRecipient, TParent>> branch = new(this);
        actions.Add(new(guard, DelegateSignature.HasParameter | DelegateSignature.HasRecipient, branch));
        return branch;
    }

    /// <summary>
    /// Determines an action to execute when the event (or branch if this instance cames from an <see cref="If(Func{bool})"/> or any of its oveload calls) is raised.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public TransitionBuilder<TState, TEvent, TRecipient, TParent> Do(Action action)
    {
        Debug.Assert(Parent is IStateMachineBuilderReacher<TState, TEvent, TRecipient>);
        if (Unsafe.As<IStateMachineBuilderReacher<TState, TEvent, TRecipient>>(Parent).StateMachineBuilder.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        actions.Add(new(action, DelegateSignature.Empty, null));
        return this;
    }

    /// <summary>
    /// Determines an action to execute when the event (or branch if this instance cames from an <see cref="If(Func{bool})"/> or any of its oveload calls) is raised.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public TransitionBuilder<TState, TEvent, TRecipient, TParent> Do(Action<TRecipient> action)
    {
        Debug.Assert(Parent is IStateMachineBuilderReacher<TState, TEvent, TRecipient>);
        if (Unsafe.As<IStateMachineBuilderReacher<TState, TEvent, TRecipient>>(Parent).StateMachineBuilder.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        actions.Add(new(action, DelegateSignature.HasRecipient, null));
        return this;
    }

    /// <summary>
    /// Determines an action to execute when the event (or branch if this instance cames from an <see cref="If(Func{bool})"/> or any of its oveload calls) is raised.<br/>
    /// This action will only be executed if the event is fired with the specific <typeparamref name="TParameter"/> type.
    /// </summary>
    /// <typeparam name="TParameter">Type of parameter passed to the action when a trigger is fired.</typeparam>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public TransitionBuilder<TState, TEvent, TRecipient, TParent> Do<TParameter>(Action<TParameter> action)
    {
        Debug.Assert(Parent is IStateMachineBuilderReacher<TState, TEvent, TRecipient>);
        if (Unsafe.As<IStateMachineBuilderReacher<TState, TEvent, TRecipient>>(Parent).StateMachineBuilder.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        actions.Add(new(action, DelegateSignature.HasParameter, null));
        return this;
    }

    /// <summary>
    /// Determines an action to execute when the event (or branch if this instance cames from an <see cref="If(Func{bool})"/> or any of its oveload calls) is raised.<br/>
    /// This action will only be executed if the event is fired with the specific <typeparamref name="TParameter"/> type.
    /// </summary>
    /// <typeparam name="TParameter">Type of parameter passed to the action when a trigger is fired.</typeparam>
    /// <param name="action">Action to execute.</param>
    /// <returns><see langword="this"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public TransitionBuilder<TState, TEvent, TRecipient, TParent> Do<TParameter>(Action<TRecipient, TParameter> action)
    {
        Debug.Assert(Parent is IStateMachineBuilderReacher<TState, TEvent, TRecipient>);
        if (Unsafe.As<IStateMachineBuilderReacher<TState, TEvent, TRecipient>>(Parent).StateMachineBuilder.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (action is null) ThrowHelper.ThrowArgumentNullException_Action();
        actions.Add(new(action, DelegateSignature.HasRecipient | DelegateSignature.HasParameter, null));
        return this;
    }

    /// <inheritdoc cref="GotoBuilder{TState, TEvent, TRecipient, TParent}.OnEntryPolicy(TransitionPolicy)"/>
    public GotoBuilder<TState, TEvent, TRecipient, TParent> OnEntryPolicy(TransitionPolicy policy)
    {
        Debug.Assert(Parent is IStateMachineBuilderReacher<TState, TEvent, TRecipient>);
        if (Unsafe.As<IStateMachineBuilderReacher<TState, TEvent, TRecipient>>(Parent).StateMachineBuilder.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        GotoBuilder<TState, TEvent, TRecipient, TParent> builder = new(this);
        actions.Add(new(null, default, builder));
        builder.OnEntryPolicy(policy);
        return builder;
    }

    /// <inheritdoc cref="GotoBuilder{TState, TEvent, TRecipient, TParent}.OnEntryPolicy(TransitionPolicy)"/>
    public GotoBuilder<TState, TEvent, TRecipient, TParent> OnExitPolicy(TransitionPolicy policy)
    {
        Debug.Assert(Parent is IStateMachineBuilderReacher<TState, TEvent, TRecipient>);
        if (Unsafe.As<IStateMachineBuilderReacher<TState, TEvent, TRecipient>>(Parent).StateMachineBuilder.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        GotoBuilder<TState, TEvent, TRecipient, TParent> builder = new(this);
        actions.Add(new(null, default, builder));
        builder.OnExitPolicy(policy);
        return builder;
    }

    /// <summary>
    /// Determines to which state this transition goes.<br/>
    /// This is equivalent to <c>OnEntryPolicy(TransitionPolicy.ChildFirstWithCulling).OnExitPolicy(TransitionPolicy.ParentFirstWithCulling).Goto(state)</c>.
    /// </summary>
    /// <param name="state">State to move</param>
    /// <returns>Creator of this instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.<br/>
    /// Thrown when <paramref name="state"/> is <see langword="null"/>.</exception>
    public TParent Goto(TState state)
    {
        Debug.Assert(Parent is IStateMachineBuilderReacher<TState, TEvent, TRecipient>);
        if (Unsafe.As<IStateMachineBuilderReacher<TState, TEvent, TRecipient>>(Parent).StateMachineBuilder.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        if (state is null) ThrowHelper.ThrowArgumentNullException_State();
        GotoBuilder<TState, TEvent, TRecipient, TParent> builder = new(this);
        actions.Add(new(null, default, builder));
        builder.Goto(state);
        return Parent;
    }

    /// <summary>
    /// Determines to transite to the current state (reentrant).<br/>
    /// If <paramref name="runParentsActions"/> is <see langword="false"/> on exit and on entry actions of current state (but not parent states in case of current state being a substate) will be executed.<br/>
    /// This is equivalent to <c>OnEntryPolicy(TransitionPolicy.ChildFirstWithCullingInclusive).OnExitPolicy(TransitionPolicy.ParentFirstWithCullingInclusive).Goto(currentState)</c>.<br/>
    /// If <paramref name="runParentsActions"/> is <see langword="true"/> on exit and on entry actions of the current state (and parents in case of current state being a substate) will be executed.<br/>
    /// This is equivalent to <c>OnEntryPolicy(TransitionPolicy.ChildFirst).OnExitPolicy(TransitionPolicy.ParentFirst).Goto(currentState)</c>.<br/>
    /// </summary>
    /// <param name="runParentsActions">Determines if parent(s) actions should be executed or not in case of being a substate.</param>
    /// <returns>Creator of this instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.</exception>
    public TParent GotoSelf(bool runParentsActions = false)
    {
        Debug.Assert(Parent is IStateMachineBuilderReacher<TState, TEvent, TRecipient>);
        if (Unsafe.As<IStateMachineBuilderReacher<TState, TEvent, TRecipient>>(Parent).StateMachineBuilder.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        GotoBuilder<TState, TEvent, TRecipient, TParent> builder = new(this);
        actions.Add(new(null, default, builder));
        if (runParentsActions)
            builder.OnEntryPolicy(TransitionPolicy.ChildFirst).OnExitPolicy(TransitionPolicy.ParentFirst);
        else
            builder.OnEntryPolicy(TransitionPolicy.ChildFirstWithCullingInclusive).OnExitPolicy(TransitionPolicy.ParentFirstWithCullingInclusive);
        builder.GotoSelf();
        return Parent;
    }

    /// <summary>
    /// Determines that will have no transition to any state, so no on entry nor on exit event will be raised.<br/>
    /// This is equivalent to either <c>OnEntryPolicy(TransitionPolicy.Ignore).OnExitPolicy(TransitionPolicy.Ignore).GotoSelf()</c> or <c>OnEntryPolicy(TransitionPolicy.ChildFirstWithCulling).OnExitPolicy(TransitionPolicy.ParentFirstWithCulling).GotoSelf()</c> (in a state transition of <c>Self -> Self</c> both codes produces the same effect).
    /// </summary>
    /// <returns>Creator of this instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.Finalize"/> or <see cref="StateBuilder{TState, TEvent, TRecipient}.Finalize"/> has already been called in this builder's hierarchy.</exception>
    public TParent StaySelf()
    {
        Debug.Assert(Parent is IStateMachineBuilderReacher<TState, TEvent, TRecipient>);
        if (Unsafe.As<IStateMachineBuilderReacher<TState, TEvent, TRecipient>>(Parent).StateMachineBuilder.HasFinalized) ThrowHelper.ThrowInvalidOperationException_AlreadyHasFinalized();
        GotoBuilder<TState, TEvent, TRecipient, TParent> builder = new(this);
        builder.OnEntryPolicy(TransitionPolicy.Ignore).OnExitPolicy(TransitionPolicy.Ignore);
        actions.Add(new(null, default, builder));
        builder.GotoSelf();
        return Parent;
    }

    internal int GetTotalTransitionsAndValidate(Dictionary<TState, StateBuilder<TState, TEvent, TRecipient>> states)
    {
        if (actions.Count == 0 || !actions[actions.Count - 1].IsTerminator) ThrowHelper.ThrowInvalidOperationException_TransitionMustHaveTerminator();
        int total = actions.Count;
        foreach (TransitionBuilderUnion<TState, TEvent, TRecipient> action in actions)
            total += action.GetTotalTransitionsAndValidate(states, this);
        return total;
    }

    int ITransitionBuilder<TState, TEvent, TRecipient>.GetTotalTransitionsAndEnsureHasTerminator(Dictionary<TState, StateBuilder<TState, TEvent, TRecipient>> states)
        => GetTotalTransitionsAndValidate(states);

    internal void Save(Dictionary<TState, StateBuilder<TState, TEvent, TRecipient>> states, Dictionary<TState, int> statesMap, int currentStateIndex, StateBuilder<TState, TEvent, TRecipient> currentStateBuilder, TransitionEventUnion[] transitionEvents, ref int iTransitionEvents)
    {
        int i = iTransitionEvents;
        iTransitionEvents += actions.Count;
        foreach (TransitionBuilderUnion<TState, TEvent, TRecipient> action in actions)
            iTransitionEvents += action.GetTotalTransitionsUsedInGoto(states, this);
        foreach (TransitionBuilderUnion<TState, TEvent, TRecipient> action in actions)
            action.Save(states, statesMap, currentStateIndex, currentStateBuilder, transitionEvents, ref i, ref iTransitionEvents);
    }

    void ITransitionBuilder<TState, TEvent, TRecipient>.Save(Dictionary<TState, StateBuilder<TState, TEvent, TRecipient>> states, Dictionary<TState, int> statesMap, int currentStateIndex, StateBuilder<TState, TEvent, TRecipient> currentStateBuilder, TransitionEventUnion[] transitionEvents, ref int iTransitionEvents)
        => Save(states, statesMap, currentStateIndex, currentStateBuilder, transitionEvents, ref iTransitionEvents);
}
