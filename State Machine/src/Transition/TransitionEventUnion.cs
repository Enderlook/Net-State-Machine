using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

internal readonly struct TransitionEventUnion
{
    private readonly Delegate? @delegate;
    private readonly TransitionEventType type;
    private readonly int index;

    public TransitionEventUnion(Delegate? @delegate, TransitionEventType type, int index)
    {
        this.@delegate = @delegate;
        this.type = type;
        this.index = index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (TransitionResult Result, int Index) Invoke<TRecipient>(TRecipient recipient, SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator)
    {
        switch (type)
        {
            case TransitionEventType.Empty:
                Debug.Assert(@delegate is Action);
                Unsafe.As<Action>(@delegate)();
                goto continue_;
            case TransitionEventType.HasRecipient:
                Debug.Assert(@delegate is Action<TRecipient>);
                Unsafe.As<Action<TRecipient>>(@delegate)(recipient);
                goto continue_;
            case TransitionEventType.HasParameter:
                Debug.Assert(@delegate is not null);
                if (parametersEnumerator.Has)
                {
                    do
                    {
                        if (parametersEnumerator.Current.TryRun(@delegate))
                            goto continue_;
                    } while (parametersEnumerator.Next());
                }
                goto continue_;
            case TransitionEventType.HasRecipient | TransitionEventType.HasParameter:
                Debug.Assert(@delegate is not null);
                if (parametersEnumerator.Has)
                {
                    do
                    {
                        if (parametersEnumerator.Current.TryRun(@delegate, recipient))
                            goto continue_;
                    } while (parametersEnumerator.Next());
                }
                goto continue_;
            case TransitionEventType.IsBranch | TransitionEventType.Empty:
                Debug.Assert(@delegate is Func<bool>);
                if (Unsafe.As<Func<bool>>(@delegate)())
                    goto branch;
                goto continue_;
            case TransitionEventType.IsBranch | TransitionEventType.HasRecipient:
                Debug.Assert(@delegate is Func<TRecipient, bool>);
                if (Unsafe.As<Func<TRecipient, bool>>(@delegate)(recipient))
                    goto branch;
                goto continue_;
            case TransitionEventType.IsBranch | TransitionEventType.HasParameter:
                Debug.Assert(@delegate is not null);
                if (parametersEnumerator.Has)
                {
                    do
                    {
                        if (parametersEnumerator.Current.TryRun(@delegate, out bool mustBranch))
                        {
                            if (mustBranch)
                                goto branch;
                            else
                                goto continue_;
                        }
                    } while (parametersEnumerator.Next());
                }
                goto continue_;
            case TransitionEventType.IsBranch | TransitionEventType.HasRecipient | TransitionEventType.HasParameter:
                Debug.Assert(@delegate is not null);
                if (parametersEnumerator.Has)
                {
                    do
                    {
                        if (parametersEnumerator.Current.TryRun(@delegate, recipient, out bool mustBranch))
                        {
                            if (mustBranch)
                                goto branch;
                            else
                                goto continue_;
                        }
                    } while (parametersEnumerator.Next());
                }
                goto continue_;
            case TransitionEventType.IsGoTo:
                return (TransitionResult.GoTo, index);
            case TransitionEventType.IsStaySelf:
                return (TransitionResult.StaySelf, default);
            default:
                Debug.Fail("Impossible state");
                goto case TransitionEventType.Empty;
        }

    continue_:
        return (TransitionResult.Continue, default);
    branch:
        return (TransitionResult.Branch, index);
    }
}
