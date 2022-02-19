using System;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

internal sealed class ParameterSlots<TParameter> : ParameterSlots
{
    private SlotsQueue<TParameter> queue;

    public ParameterSlots() => queue = new(1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)] // Required for constant propagation of parameter
    public int Store(TParameter parameter, bool connectToPrevious = true) => queue.StoreLast(parameter, connectToPrevious);

    public override void Remove(int slot) => queue.Remove(slot);

    public override bool TryRun(int slot, Delegate @delegate)
    {
        if (@delegate is Action<TParameter> action)
        {
            action(queue[slot]);
            return true;
        }
        return false;
    }

    public override bool TryRun<TRecipient>(int slot, Delegate @delegate, TRecipient recipient)
    {
        if (@delegate is Action<TRecipient, TParameter> action)
        {
            action(recipient, queue[slot]);
            return true;
        }
        return false;
    }

    public override bool TryRun(int slot, Delegate @delegate, out bool isTrue)
    {
        if (@delegate is Func<TParameter, bool> func)
        {
            isTrue = func(queue[slot]);
            return true;
        }
#if NET5_0_OR_GREATER
            Unsafe.SkipInit(out isTrue);
#else
            isTrue = default;
#endif
        return false;
    }

    public override bool TryRun<TRecipient>(int slot, Delegate @delegate, TRecipient recipient, out bool isTrue)
    {
        if (@delegate is Func<TRecipient, TParameter, bool> func)
        {
            isTrue = func(recipient, queue[slot]);
            return true;
        }
#if NET5_0_OR_GREATER
            Unsafe.SkipInit(out isTrue);
#else
            isTrue = default;
#endif
        return false;
    }
}
