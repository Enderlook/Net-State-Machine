using System;

namespace Enderlook.StateMachine;

internal abstract class StateHolder<TRecipient> : StateHolder
{
    public abstract void Invoke(TRecipient recipient, Delegate @delegate);

    public abstract void TryRun(Delegate action, SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator);

    public abstract void TryRun(Delegate action, TRecipient recipient, SlotsQueue<ParameterSlot>.Enumerator parametersEnumerator);
}