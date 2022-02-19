﻿using System;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Invoke<TRecipient>(TRecipient recipient, SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator)
    {
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
                Debug.Assert(action is not null);
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
                Debug.Assert(action is not null);
                if (parametersEnumerator.Has)
                {
                    do
                    {
                        if (parametersEnumerator.Current.TryRun(action, recipient))
                            break;
                    } while (parametersEnumerator.Next());
                }
                break;
            default:
                Debug.Fail("Impossible state.");
                goto case StateEventType.Empty;
        }
    }
}
