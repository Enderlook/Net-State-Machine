using System;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

internal readonly struct ParameterSlot
{
    private readonly ParameterSlots container;
    private readonly int slot;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ParameterSlot(ParameterSlots container, int slot)
    {
        this.container = container;
        this.slot = slot;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRun(Delegate action) => container.TryRun(slot, action);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRun<T>(Delegate action, T arg) => container.TryRun(slot, action, arg);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRun<T, U>(Delegate action, T arg1, U arg2) => container.TryRun(slot, action, arg1, arg2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRun(Delegate func, out bool isTrue) => container.TryRun(slot, func, out isTrue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRun<T>(Delegate func, T arg, out bool isTrue) => container.TryRun(slot, func, arg, out isTrue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove() => container.Remove(slot);
}
