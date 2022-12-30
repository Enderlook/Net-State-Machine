using System;

namespace Enderlook.StateMachine;

internal abstract class ParameterSlots
{
    public abstract bool TryRun(int slot, Delegate action);

    public abstract bool TryRun<T>(int slot, Delegate action, T arg);

    public abstract bool TryRun<T, U>(int slot, Delegate action, T arg1, U arg2);

    public abstract bool TryRun(int slot, Delegate func, out bool isTrue);

    public abstract bool TryRun<T>(int slot, Delegate func, T arg, out bool isTrue);

    public abstract void Remove(int slot);
}
