using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

internal readonly struct StateEventUnion
{
    private readonly Delegate action;
    private readonly StateEventType type;

    public StateEventUnion(Delegate action, StateEventType type)
    {
        this.action = action;
        this.type = type;
    }

    public TransitionEventUnion ToTransitionEvent()
    {
        TransitionEventType type;
        switch (this.type)
        {
            case StateEventType.Empty:
                type = TransitionEventType.Empty;
                break;
            case StateEventType.HasRecipient:
                type = TransitionEventType.HasRecipient;
                break;
            case StateEventType.HasParameter:
                type = TransitionEventType.HasParameter;
                break;
            case StateEventType.HasRecipient | StateEventType.HasParameter:
                type = TransitionEventType.HasRecipient | TransitionEventType.HasParameter;
                break;
            default:
                Debug.Fail("Impossible state");
                goto case StateEventType.Empty;
        }
        return new(action, type, default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Invoke<TRecipient>(TRecipient recipient, object? stateRecipient, StateRecipientType stateRecipientType, StateHelper? stateHelper, SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator)
    {
        Debug.Assert(action is not null);
        Debug.Assert(stateRecipientType == StateRecipientType.Unused ? stateRecipient is null : stateRecipientType == StateRecipientType.ValueType == typeof(StateHolder<TRecipient>).IsAssignableFrom(stateRecipient.GetType()));
        Debug.Assert((stateRecipientType == StateRecipientType.ReferenceType) == stateHelper is not null);
        switch (type)
        {
            case StateEventType.Empty:
                Debug.Assert(action is Action);
                Unsafe.As<Action>(action)();
                break;
            case StateEventType.HasRecipient:
                Debug.Assert(action is Action<TRecipient>);
                Unsafe.As<Action<TRecipient>>(action)(recipient);
                break;
            case StateEventType.HasParameter:
                if (parametersEnumerator.Has)
                {
                    do
                    {
                        if (parametersEnumerator.Current.TryRun(action))
                            break;
                    } while (parametersEnumerator.Next());
                }
                break;
            case StateEventType.HasRecipient | StateEventType.HasParameter:
                if (parametersEnumerator.Has)
                {
                    do
                    {
                        if (parametersEnumerator.Current.TryRun(action, recipient))
                            break;
                    } while (parametersEnumerator.Next());
                }
                break;
            case StateEventType.HasStateRecipient when stateRecipientType != StateRecipientType.Unused:
                if (stateRecipientType == StateRecipientType.ReferenceType)
                {
                    Debug.Assert(action.GetType().GetGenericTypeDefinition() == typeof(Action<>));
                    Debug.Assert(stateRecipient is null || action.GetType().GetGenericArguments()[0].IsAssignableFrom(stateRecipient.GetType()));
                    Unsafe.As<Action<object?>>(action)(stateRecipient);
                }
                else
                {
                    Debug.Assert(stateRecipient is not null);
                    Unsafe.As<StateHolder<TRecipient>>(stateRecipient).Invoke(action);
                }
                break;
            case StateEventType.HasStateRecipient | StateEventType.HasParameter when stateRecipientType != StateRecipientType.Unused:
                if (stateRecipientType == StateRecipientType.ReferenceType)
                {
                    Debug.Assert(action.GetType().GetGenericTypeDefinition() == typeof(Action<,>));
                    Debug.Assert(action.GetType().GetGenericArguments()[0] == typeof(TRecipient));
                    Debug.Assert(stateRecipient is null || action.GetType().GetGenericArguments()[1].IsAssignableFrom(stateRecipient.GetType()));
                    Unsafe.As<Action<TRecipient, object?>>(action)(recipient, stateRecipient);
                }
                else
                {
                    Debug.Assert(stateRecipient is not null);
                    Unsafe.As<StateHolder<TRecipient>>(stateRecipient).Invoke(recipient, action);
                }
                break;
            case StateEventType.HasStateRecipient | StateEventType.HasParameter when stateRecipientType != StateRecipientType.Unused:
                if (stateRecipientType == StateRecipientType.ReferenceType)
                {
                    Debug.Assert(stateHelper is not null);
                    stateHelper.TryRun<TRecipient>(action, stateRecipient, parametersEnumerator);
                }
                else
                {
                    Debug.Assert(stateRecipient is StateHolder<TRecipient>);
                    Unsafe.As<StateHolder<TRecipient>>(stateRecipient).TryRun(action, parametersEnumerator);
                }
                break;
            case StateEventType.HasStateRecipient | StateEventType.HasRecipient | StateEventType.HasParameter when stateRecipientType != StateRecipientType.Unused:
                if (stateRecipientType == StateRecipientType.ReferenceType)
                {
                    Debug.Assert(stateHelper is not null);
                    stateHelper.TryRun<TRecipient>(action, recipient, stateRecipient, parametersEnumerator);
                }
                else
                {
                    Debug.Assert(stateRecipient is StateHolder<TRecipient>);
                    Unsafe.As<StateHolder<TRecipient>>(stateRecipient).TryRun(action, recipient, parametersEnumerator);
                }
                break;
            default:
                Debug.Fail("Impossible state.");
                goto case StateEventType.Empty;
        }
    }
}
