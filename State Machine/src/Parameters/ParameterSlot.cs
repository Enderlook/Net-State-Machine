using System;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

internal readonly struct ParameterSlot
{
    private readonly ParameterSlots container;
    private readonly int slot;

    public ParameterSlot(ParameterSlots container, int slot)
    {
        this.container = container;
        this.slot = slot;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRun(Delegate @delegate) => container.TryRun(slot, @delegate);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRun<TRecipient>(Delegate @delegate, TRecipient recipient) => container.TryRun(slot, @delegate, recipient);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRun(Delegate @delegate, out bool isTrue) => container.TryRun(slot, @delegate, out isTrue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRun<TRecipient>(Delegate @delegate, TRecipient recipient, out bool isTrue) => container.TryRun(slot, @delegate, recipient, out isTrue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove() => container.Remove(slot);
}
