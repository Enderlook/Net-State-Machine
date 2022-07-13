using System;

namespace Enderlook.StateMachine;

internal abstract class ParameterSlots
{
    public abstract bool TryRun(int slot, Delegate @delegate);

    public abstract bool TryRun<TRecipient>(int slot, Delegate @delegate, TRecipient recipient);

    public abstract bool TryRun(int slot, Delegate @delegate, out bool isTrue);

    public abstract bool TryRun<TRecipient>(int slot, Delegate @delegate, TRecipient recipient, out bool isTrue);

    public abstract void Remove(int slot);
}
