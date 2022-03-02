using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

internal readonly struct TransitionBuilderUnion<TState, TEvent, TRecipient>
    where TState : notnull
    where TEvent : notnull
{
    private readonly Delegate? @delegate;
    private readonly DelegateSignature delegateSignature;
    private readonly object? continuation;

    public bool IsTerminator => continuation is IGoto<TState>;

    public TransitionBuilderUnion(Delegate? @delegate, DelegateSignature type, object? continuation)
    {
        this.@delegate = @delegate;
        delegateSignature = type;
        Debug.Assert(continuation is null || continuation is IGoto<TState> || continuation is ITransitionBuilder<TState, TEvent, TRecipient>);
        this.continuation = continuation;
    }

    public int GetTotalTransitionsAndValidate(Dictionary<TState, StateBuilder<TState, TEvent, TRecipient>> states, ITransitionBuilder<TState, TEvent, TRecipient> transitionBuilder)
    {
        if (continuation is ITransitionBuilder<TState, TEvent, TRecipient> branchTo_)
            return branchTo_.GetTotalTransitionsAndEnsureHasTerminator(states);

        return GetTotalTransitionsUsedInGoto(states, transitionBuilder);
    }

    public int GetTotalTransitionsUsedInGoto(Dictionary<TState, StateBuilder<TState, TEvent, TRecipient>> states, ITransitionBuilder<TState, TEvent, TRecipient> transitionBuilder)
    {
        int transitions = 0;
        if (continuation is IGoto<TState> @goto)
        {
            {
                if (@goto.State is TState state && !states.ContainsKey(state)) ThrowHelper.ThrowInvalidOperationException_TransitionGoesToANotRegisteredState();
            }

            switch (@goto.OnExitPolicy)
            {
                case TransitionPolicy.Ignore:
                    break;
                case TransitionPolicy.ChildFirst:
                case TransitionPolicy.ParentFirst:
                {
                    StateBuilder<TState, TEvent, TRecipient> stateBuilder = transitionBuilder.StateBuilder;
                    while (true)
                    {
                        transitions += stateBuilder.OnExitCount;
                        if (!stateBuilder.IsSubState(out TState? state))
                            break;
                        stateBuilder = states[state];
                    }
                    break;
                }
                case TransitionPolicy.ChildFirstWithCulling:
                case TransitionPolicy.ParentFirstWithCulling:
                {
                    if (@goto.State is not TState gotoState)
                        // If true, this is a GotoSelf, which means the operation below would yield 0.
                        break;

                    StateBuilder<TState, TEvent, TRecipient> stateBuilder = transitionBuilder.StateBuilder;
                    while (CheckRepetition(states, stateBuilder, gotoState))
                    {
                        transitions += stateBuilder.OnExitCount;
                        if (!stateBuilder.IsSubState(out TState? state))
                            break;
                        stateBuilder = states[state];
                    }
                    break;
                }
                case TransitionPolicy.ParentFirstWithCullingInclusive:
                case TransitionPolicy.ChildFirstWithCullingInclusive:
                {
                    if (@goto.State is not TState gotoState)
                    {
                        // If true, this is GotoSelf, which means the operation below can be simplified to:
                        transitions += transitionBuilder.StateBuilder.OnExitCount;
                        break;
                    }

                    StateBuilder<TState, TEvent, TRecipient> stateBuilder = transitionBuilder.StateBuilder;
                    while (true)
                    {
                        if (!CheckRepetition(states, stateBuilder, gotoState))
                        {
                            transitions += stateBuilder.OnExitCount;
                            goto exit;
                        }

                        transitions += stateBuilder.OnExitCount;
                        if (!stateBuilder.IsSubState(out TState? state))
                            break;
                        stateBuilder = states[state];
                    }
                exit:;
                    break;
                }
                default:
                    Debug.Fail("Impossible state.");
                    goto case TransitionPolicy.Ignore;
            }

            switch (@goto.OnEntryPolicy)
            {
                case TransitionPolicy.Ignore:
                    break;
                case TransitionPolicy.ChildFirst:
                case TransitionPolicy.ParentFirst:
                {
                    StateBuilder<TState, TEvent, TRecipient> stateBuilder;
                    if (@goto.State is TState gotoState)
                        stateBuilder = states[gotoState];
                    else
                        stateBuilder = transitionBuilder.StateBuilder;

                    while (true)
                    {
                        transitions += stateBuilder.OnEntryCount;
                        if (!stateBuilder.IsSubState(out TState? state))
                            break;
                        stateBuilder = states[state];
                    }
                    break;
                }
                case TransitionPolicy.ChildFirstWithCulling:
                case TransitionPolicy.ParentFirstWithCulling:
                {
                    if (@goto.State is not TState gotoState)
                        // If true, this is a GotoSelf, which means the operation below would yield 0.
                        break;

                    StateBuilder<TState, TEvent, TRecipient> stateBuilder = states[gotoState];
                    while (CheckRepetition(states, stateBuilder, gotoState))
                    {
                        transitions += stateBuilder.OnEntryCount;
                        if (!stateBuilder.IsSubState(out TState? state))
                            break;
                        stateBuilder = states[state];
                    }
                    break;
                }
                case TransitionPolicy.ParentFirstWithCullingInclusive:
                case TransitionPolicy.ChildFirstWithCullingInclusive:
                {
                    if (@goto.State is not TState gotoState)
                    {
                        // If true, this is GotoSelf, which means the operation below can be simplified to:
                        transitions += transitionBuilder.StateBuilder.OnEntryCount;
                        break;
                    }

                    StateBuilder<TState, TEvent, TRecipient> stateBuilder = states[gotoState];
                    while (true)
                    {
                        if (!CheckRepetition(states, stateBuilder, gotoState))
                        {
                            transitions += stateBuilder.OnEntryCount;
                            goto exit;
                        }

                        transitions += stateBuilder.OnEntryCount;
                        if (!stateBuilder.IsSubState(out TState? state))
                            break;
                        stateBuilder = states[state];
                    }
                exit:;
                    break;
                }
                default:
                    Debug.Fail("Impossible state.");
                    goto case TransitionPolicy.Ignore;
            }
        }
        return transitions;
    }

    public void Save(Dictionary<TState, StateBuilder<TState, TEvent, TRecipient>> states, Dictionary<TState, int> statesMap, int currentStateIndex, StateBuilder<TState, TEvent, TRecipient> currentStateBuilder, TransitionEventUnion[] transitionEvents, ref int i, ref int iTransitionEvents)
    {
        int index;
        TransitionEventType type_;
        if (continuation is IGoto<TState> @goto)
        {
            type_ = TransitionEventType.IsGoTo;
            {
                TState? state = @goto.State;
                if (state is null)
                    index = currentStateIndex; // Is GotoSelf.
                else
                    index = statesMap[state];
            }

            switch (@goto.OnExitPolicy)
            {
                case TransitionPolicy.Ignore:
                    break;
                case TransitionPolicy.ChildFirst:
                {
                    StateBuilder<TState, TEvent, TRecipient> stateBuilder = currentStateBuilder;
                    while (true)
                    {
                        StoreInTransitionEvents(stateBuilder.GetOnExitEnumerator(), ref i);
                        if (!stateBuilder.IsSubState(out TState? state))
                            break;
                        stateBuilder = states[state];
                    }
                    break;
                }
                case TransitionPolicy.ParentFirst:
                {
                    Traverse(currentStateBuilder, ref i);
                    break;

                    void Traverse(StateBuilder<TState, TEvent, TRecipient> stateBuilder, ref int i)
                    {
                        if (stateBuilder.IsSubState(out TState? state))
                            Traverse(states[state], ref i);
                        StoreInTransitionEvents(stateBuilder.GetOnExitEnumerator(), ref i);
                    }
                }
                case TransitionPolicy.ChildFirstWithCulling:
                {
                    if (@goto.State is not TState gotoState)
                        // If true, this is a GotoSelf, which means the operation below would not add any transition event.
                        break;

                    StateBuilder<TState, TEvent, TRecipient> stateBuilder = currentStateBuilder;
                    while (CheckRepetition(states, stateBuilder, gotoState))
                    {
                        StoreInTransitionEvents(stateBuilder.GetOnExitEnumerator(), ref i);
                        if (!stateBuilder.IsSubState(out TState? state))
                            break;
                        stateBuilder = states[state];
                    }
                    break;
                }
                case TransitionPolicy.ParentFirstWithCulling:
                {
                    if (@goto.State is not TState gotoState)
                        // If true, this is a GotoSelf, which means the operation below would not add any transition event.
                        break;

                    Traverse(currentStateBuilder, ref i);
                    break;

                    void Traverse(StateBuilder<TState, TEvent, TRecipient> stateBuilder, ref int i)
                    {
                        if (!CheckRepetition(states, stateBuilder, gotoState))
                            return;

                        if (stateBuilder.IsSubState(out TState? state))
                            Traverse(states[state], ref i);
                        StoreInTransitionEvents(stateBuilder.GetOnExitEnumerator(), ref i);
                    }
                }
                case TransitionPolicy.ChildFirstWithCullingInclusive:
                {
                    if (@goto.State is not TState gotoState)
                    {
                        // If true, this is GotoSelf, which means the operation below can be simplified to:
                        StoreInTransitionEvents(currentStateBuilder.GetOnExitEnumerator(), ref i);
                        break;
                    }

                    StateBuilder<TState, TEvent, TRecipient> stateBuilder = currentStateBuilder;
                    while (true)
                    {
                        if (!CheckRepetition(states, stateBuilder, gotoState))
                        {
                            StoreInTransitionEvents(stateBuilder.GetOnExitEnumerator(), ref i);
                            goto exit;
                        }

                        StoreInTransitionEvents(stateBuilder.GetOnExitEnumerator(), ref i);
                        if (!stateBuilder.IsSubState(out TState? state))
                            break;
                        stateBuilder = states[state];
                    }
                exit:
                    break;
                }
                case TransitionPolicy.ParentFirstWithCullingInclusive:
                {
                    if (@goto.State is not TState gotoState)
                    {
                        // If true, this is GotoSelf, which means the operation below can be simplified to:
                        StoreInTransitionEvents(currentStateBuilder.GetOnExitEnumerator(), ref i);
                        break;
                    }

                    Traverse(currentStateBuilder, ref i);
                    break;

                    void Traverse(StateBuilder<TState, TEvent, TRecipient> stateBuilder, ref int i)
                    {
                        if (!CheckRepetition(states, stateBuilder, gotoState))
                        {
                            StoreInTransitionEvents(stateBuilder.GetOnExitEnumerator(), ref i);
                            return;
                        }

                        if (stateBuilder.IsSubState(out TState? state))
                            Traverse(states[state], ref i);
                        StoreInTransitionEvents(stateBuilder.GetOnExitEnumerator(), ref i);
                    }
                }
                default:
                    Debug.Fail("Impossible state.");
                    goto case TransitionPolicy.Ignore;
            }

            switch (@goto.OnEntryPolicy)
            {
                case TransitionPolicy.Ignore:
                    break;
                case TransitionPolicy.ChildFirst:
                {
                    StateBuilder<TState, TEvent, TRecipient> stateBuilder;
                    if (@goto.State is TState gotoState)
                        stateBuilder = states[gotoState];
                    else
                        stateBuilder = currentStateBuilder;

                    while (true)
                    {
                        StoreInTransitionEvents(stateBuilder.GetOnEntryEnumerator(), ref i);
                        if (!stateBuilder.IsSubState(out TState? state))
                            break;
                        stateBuilder = states[state];
                    }
                    break;
                }
                case TransitionPolicy.ParentFirst:
                {
                    StateBuilder<TState, TEvent, TRecipient> stateBuilder;
                    if (@goto.State is TState gotoState)
                        stateBuilder = states[gotoState];
                    else
                        stateBuilder = currentStateBuilder;

                    Traverse(stateBuilder, ref i);
                    break;

                    void Traverse(StateBuilder<TState, TEvent, TRecipient> stateBuilder, ref int i)
                    {
                        if (stateBuilder.IsSubState(out TState? state))
                            Traverse(states[state], ref i);
                        StoreInTransitionEvents(stateBuilder.GetOnEntryEnumerator(), ref i);
                    }
                }
                case TransitionPolicy.ChildFirstWithCulling:
                {
                    if (@goto.State is not TState gotoState)
                        // If true, this is a GotoSelf, which means the operation below would not add any.
                        break;

                    StateBuilder<TState, TEvent, TRecipient> stateBuilder = states[gotoState];
                    while (CheckRepetition(states, stateBuilder, gotoState))
                    {
                        StoreInTransitionEvents(stateBuilder.GetOnEntryEnumerator(), ref i);
                        if (!stateBuilder.IsSubState(out TState? state))
                            break;
                        stateBuilder = states[state];
                    }
                    break;
                }
                case TransitionPolicy.ParentFirstWithCulling:
                {
                    if (@goto.State is not TState gotoState)
                        // If true, this is a GotoSelf, which means the operation below would not add any.
                        break;

                    Traverse(states[gotoState], ref i);
                    break;

                    void Traverse(StateBuilder<TState, TEvent, TRecipient> stateBuilder, ref int i)
                    {
                        if (!CheckRepetition(states, stateBuilder, gotoState))
                            return;

                        if (stateBuilder.IsSubState(out TState? state))
                            Traverse(states[state], ref i);
                        StoreInTransitionEvents(stateBuilder.GetOnEntryEnumerator(), ref i);
                    }
                }
                case TransitionPolicy.ChildFirstWithCullingInclusive:
                {
                    if (@goto.State is not TState gotoState)
                    {
                        // If true, this is GotoSelf, which means the operation below can be simplified to:
                        StoreInTransitionEvents(currentStateBuilder.GetOnEntryEnumerator(), ref i);
                        break;
                    }

                    StateBuilder<TState, TEvent, TRecipient> stateBuilder = states[gotoState];
                    while (true)
                    {
                        if (!CheckRepetition(states, stateBuilder, gotoState))
                        {
                            StoreInTransitionEvents(stateBuilder.GetOnEntryEnumerator(), ref i);
                            break;
                        }

                        StoreInTransitionEvents(stateBuilder.GetOnEntryEnumerator(), ref i);
                        if (!stateBuilder.IsSubState(out TState? state))
                            break;
                        stateBuilder = states[state];
                    }
                    break;
                }
                case TransitionPolicy.ParentFirstWithCullingInclusive:
                {
                    if (@goto.State is not TState gotoState)
                    {
                        // If true, this is GotoSelf, which means the operation below can be simplified to:
                        StoreInTransitionEvents(currentStateBuilder.GetOnEntryEnumerator(), ref i);
                        break;
                    }

                    Traverse(states[gotoState], ref i);
                    break;

                    void Traverse(StateBuilder<TState, TEvent, TRecipient> stateBuilder, ref int i)
                    {
                        if (!CheckRepetition(states, stateBuilder, gotoState))
                        {
                            StoreInTransitionEvents(stateBuilder.GetOnEntryEnumerator(), ref i);
                            return;
                        }

                        if (stateBuilder.IsSubState(out TState? state))
                            Traverse(states[state], ref i);
                        StoreInTransitionEvents(stateBuilder.GetOnEntryEnumerator(), ref i);
                    }
                }
                default:
                    Debug.Fail("Impossible state.");
                    goto case TransitionPolicy.Ignore;
            }
        }
        else if (continuation is ITransitionBuilder<TState, TEvent, TRecipient> branchTo)
        {
            type_ = TransitionEventType.IsBranch;
            if ((delegateSignature & DelegateSignature.HasRecipient) != 0)
                type_ |= TransitionEventType.HasRecipient;
            if ((delegateSignature & DelegateSignature.HasParameter) != 0)
                type_ |= TransitionEventType.HasParameter;
            index = iTransitionEvents;
            branchTo.Save(states, statesMap, currentStateIndex, currentStateBuilder, transitionEvents, ref iTransitionEvents);
        }
        else
        {
            type_ = TransitionEventType.Empty;
            if ((delegateSignature & DelegateSignature.HasRecipient) != 0)
                type_ |= TransitionEventType.HasRecipient;
            if ((delegateSignature & DelegateSignature.HasParameter) != 0)
                type_ |= TransitionEventType.HasParameter;
            index = default;
        }

        transitionEvents[i++] = new TransitionEventUnion(@delegate, type_, index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void StoreInTransitionEvents(List<StateEventUnion>.Enumerator? enumerator, ref int i)
        {
            if (enumerator is List<StateEventUnion>.Enumerator enumerator_)
            {
                using List<StateEventUnion>.Enumerator enumerator__ = enumerator_;
                while (enumerator__.MoveNext())
                    transitionEvents[i++] = enumerator__.Current.ToTransitionEvent();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CheckRepetition(Dictionary<TState, StateBuilder<TState, TEvent, TRecipient>> states, StateBuilder<TState, TEvent, TRecipient> stateBuilder, TState gotoState)
    {
        StateBuilder<TState, TEvent, TRecipient> gotoStateBuilder = states[gotoState];
        while (true)
        {
            if (gotoStateBuilder == stateBuilder)
                return false;
            if (!gotoStateBuilder.IsSubState(out TState? gotoState_))
                break;
            gotoStateBuilder = states[gotoState_];
        }
        return true;
    }
}
