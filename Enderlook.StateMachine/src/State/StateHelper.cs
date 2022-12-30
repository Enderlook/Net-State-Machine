using System;

namespace Enderlook.StateMachine;

internal abstract class StateHelper
{
    public abstract object? CreateStateRecipient<TRecipient>(TRecipient recipient, Delegate? stateRecipientFactory);

    public abstract void TryRun<TRecipient>(Delegate action, object? stateRecipient, SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator);

    public abstract void TryRun<TRecipient>(Delegate action, TRecipient recipient, object? stateRecipient, SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator);
}
