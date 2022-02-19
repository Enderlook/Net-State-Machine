using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Enderlook.StateMachine;

internal readonly struct TransitionBuilderUnion<TState, TEvent, TRecipient>
    where TState : notnull
    where TEvent : notnull
{
    private readonly Delegate? @delegate;
    private readonly TransitionEventBuilderType type;
    private readonly ITransitionBuilder<TState>? branchTo;
    private readonly TState? state;

    public bool IsTerminator => (type & (TransitionEventBuilderType.IsGoTo | TransitionEventBuilderType.IsGoToSelf | TransitionEventBuilderType.IsStaySelf)) != 0;

    public TransitionBuilderUnion(Delegate? @delegate, TransitionEventBuilderType type, ITransitionBuilder<TState>? branchTo, TState? state)
    {
        this.@delegate = @delegate;
        this.type = type;
        this.branchTo = branchTo;
        this.state = state;
    }

    public int GetTotalTransitionsAndEnsureHasTerminator()
    {
        Debug.Assert((type & TransitionEventBuilderType.IsBranch) != 0 == branchTo is not null);
        if (branchTo is ITransitionBuilder<TState> branchTo_)
            return branchTo_.GetTotalTransitionsAndEnsureHasTerminator();
        return 0;
    }

    public void Save(Dictionary<TState, int> statesMap, int currentState, TransitionEventUnion[] transitionEvents, ref int i, ref int iTransitionEvents)
    {
        int index;
        TransitionEventType type_;
        if ((type & TransitionEventBuilderType.IsGoTo) != 0)
        {
            type_ = TransitionEventType.IsGoTo;
            Debug.Assert(state is not null);
            if (!statesMap.TryGetValue(state, out index))
                ThrowHelper.ThrowInvalidOperationException_TransitionGoesToANotRegisteredState();
        }
        else if ((type & TransitionEventBuilderType.IsGoToSelf) != 0)
        {
            type_ = TransitionEventType.IsGoTo;
            index = currentState;
        }
        else if ((type & TransitionEventBuilderType.IsBranch) != 0)
        {
            type_ = TransitionEventType.IsBranch;
            if ((type & TransitionEventBuilderType.HasRecipient) != 0)
                type_ |= TransitionEventType.HasRecipient;
            if ((type & TransitionEventBuilderType.HasParameter) != 0)
                type_ |= TransitionEventType.HasParameter;
            Debug.Assert(branchTo is not null);
            index = iTransitionEvents;
            branchTo.Save(statesMap, currentState, transitionEvents, ref iTransitionEvents);
        }
        else
        {
            type_ = TransitionEventType.Empty;
            if ((type & TransitionEventBuilderType.HasRecipient) != 0)
                type_ |= TransitionEventType.HasRecipient;
            if ((type & TransitionEventBuilderType.HasParameter) != 0)
                type_ |= TransitionEventType.HasParameter;
            index = default;
        }

        transitionEvents[i++] = new TransitionEventUnion(@delegate, type_, index);
    }
}
