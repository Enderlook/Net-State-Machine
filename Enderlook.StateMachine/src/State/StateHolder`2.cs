using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

internal sealed class StateHolder<TRecipient, TStateRecipient> : StateHolder<TRecipient>
{
    private readonly TStateRecipient stateRecipient;

    public StateHolder(TStateRecipient stateRecipient) => this.stateRecipient = stateRecipient;

    public override void Invoke(Delegate @delegate)
    {
        Debug.Assert(@delegate is Action<TStateRecipient>);
        Unsafe.As<Action<TStateRecipient>>(@delegate)(stateRecipient);
    }

    public override void Invoke(TRecipient recipient, Delegate @delegate)
    {
        Debug.Assert(@delegate is Action<TRecipient, TStateRecipient>);
        Unsafe.As<Action<TRecipient, TStateRecipient>>(@delegate)(recipient, stateRecipient);
    }

    public override void TryRun(Delegate action, SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator)
    {
        if (parametersEnumerator.Has)
        {
            do
            {
                if (parametersEnumerator.Current.TryRun(action, stateRecipient))
                    break;
            } while (parametersEnumerator.Next());
        }
    }

    public override void TryRun(Delegate action, TRecipient recipient, SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator)
    {
        if (parametersEnumerator.Has)
        {
            do
            {
                if (parametersEnumerator.Current.TryRun(action, recipient, stateRecipient))
                    break;
            } while (parametersEnumerator.Next());
        }
    }
}