using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

internal sealed class StateHelper<TStateRecipient> : StateHelper
{
    public static readonly StateHelper<TStateRecipient> Singlenton = new();

    public override object? CreateStateRecipient<TRecipient>(TRecipient recipient, Delegate? stateRecipientFactory)
    {
        if (stateRecipientFactory is Func<TRecipient, TStateRecipient> func2)
            return func2(recipient);
        Debug.Assert(stateRecipientFactory is Func<TStateRecipient>);
        return Unsafe.As<Func<TStateRecipient>>(stateRecipientFactory)();
    }

    public override void TryRun<TRecipient>(Delegate action, object? stateRecipient, SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator)
    {
        Debug.Assert(stateRecipient is null || typeof(TStateRecipient).IsAssignableFrom(stateRecipient.GetType()));
        TStateRecipient stateRecipient_ = Unsafe.As<object?, TStateRecipient>(ref stateRecipient);
        if (parametersEnumerator.Has)
        {
            do
            {
                if (parametersEnumerator.Current.TryRun(action, stateRecipient_))
                    break;
            } while (parametersEnumerator.Next());
        }
    }

    public override void TryRun<TRecipient>(Delegate action, TRecipient recipient, object? stateRecipient, SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator)
    {
        Debug.Assert(stateRecipient is null || typeof(TStateRecipient).IsAssignableFrom(stateRecipient.GetType()));
        TStateRecipient stateRecipient_ = Unsafe.As<object?, TStateRecipient>(ref stateRecipient);
        if (parametersEnumerator.Has)
        {
            do
            {
                if (parametersEnumerator.Current.TryRun(action, recipient, stateRecipient_))
                    break;
            } while (parametersEnumerator.Next());
        }
    }
}