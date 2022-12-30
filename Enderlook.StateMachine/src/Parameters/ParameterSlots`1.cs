using System;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

internal sealed class ParameterSlots<TParameter> : ParameterSlots
{
    private SlotsQueue<TParameter> queue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ParameterSlots() => queue = new(1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)] // Required for constant propagation of parameter
    public int Store(TParameter parameter, bool connectToPrevious = true) => queue.StoreLast(parameter, connectToPrevious);

    public override void Remove(int slot) => queue.Remove(slot);

    public override bool TryRun(int slot, Delegate action)
    {
        if (action is Action<TParameter> action_)
        {
            action_(queue[slot]);
            return true;
        }
        return false;
    }

    public override bool TryRun<TRecipient>(int slot, Delegate action, TRecipient recipient)
    {
        if (action is Action<TRecipient, TParameter> action_)
        {
            action_(recipient, queue[slot]);
            return true;
        }
        return false;
    }

    public override bool TryRun(int slot, Delegate func, out bool isTrue)
    {
        if (func is Func<TParameter, bool> func_)
        {
            isTrue = func_(queue[slot]);
            return true;
        }
#if NET5_0_OR_GREATER
        Unsafe.SkipInit(out isTrue);
#else
        isTrue = default;
#endif
        return false;
    }

    public override bool TryRun<TRecipient>(int slot, Delegate func, TRecipient recipient, out bool isTrue)
    {
        if (func is Func<TRecipient, TParameter, bool> func_)
        {
            isTrue = func_(recipient, queue[slot]);
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
