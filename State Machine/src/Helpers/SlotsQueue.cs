using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

internal struct SlotsQueue<T>
{
    private (T Value, int Next)[] queue;
    private int first;
    private int last;
    private int firstUnused;
    private int firstDefault;

    public int Last
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => last;
    }

    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => queue[index].Value;
    }

    public SlotsQueue(int initialSize)
    {
        queue = initialSize == 0 ? Array.Empty<(T Value, int Next)>() : new (T Value, int Next)[initialSize];
        first = -1;
        last = -1;
        firstUnused = -1;
        firstDefault = 0;
    }

    public void Clear()
    {
        first = last = firstUnused = -1;
        firstDefault = 0;
    }

    public void EnsureCapacity(int space)
    {
        if (space == 0)
            return;
        int original = queue.Length;
        int length = original;
        if (length == 0)
        {
            if (space == 1)
                queue = new (T Value, int Next)[1];
            else
                length = 1;
        }
        while ((length - firstDefault) < space)
            length *= 2;
        if (length != original)
            Array.Resize(ref queue, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumeratorStartingAt(int start) => new(queue, start);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(int slot)
    {
        T value;
#if NET5_0_OR_GREATER
        Unsafe.SkipInit(out value);
#else
            value = default;
#endif
        queue[slot] = (value, firstUnused);
        firstUnused = slot;
    }

    internal void RemoveFrom(int start)
    {
        int index = start;
        do
        {
            ref (T Value, int Next) slot = ref queue[index];
            int old = index;
            index = slot.Next;
            T value;
#if NET5_0_OR_GREATER
            Unsafe.SkipInit(out value);
#else
                value = default;
#endif
            slot = (value, firstUnused);
            firstUnused = old;
        }
        while (index != 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] // Required for constant propagation of parameter
    public int StoreLast(T element, bool connectToPrevious = true)
    {
        int slot = firstUnused;
        if (slot != -1)
            firstUnused = queue[slot].Next;
        else
        {
            slot = firstDefault;
            if (slot < queue.Length)
                firstDefault = slot + 1;
            else
            {
                int length = queue.Length;
                if (length == 0)
                    queue = new (T Value, int Next)[1];
                else
                    Array.Resize(ref queue, length * 2);
                slot = length;
                firstUnused = length + 1;
            }
        }

        queue[slot] = (element, -1);
        int last_ = last;
        if (connectToPrevious && last_ != -1)
            queue[last_].Next = slot;
        last = slot;
        if (first == -1)
            first = slot;

        return slot;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryDequeue([NotNullWhen(true)] out T? element)
    {
        int old = first;
        if (old == -1)
        {
#if NET5_0_OR_GREATER
            Unsafe.SkipInit(out element);
#else
                element = default;
#endif
            return false;
        }

        ref (T Value, int Next) slot = ref queue[old];
        element = slot.Value!;
        int next = first = slot.Next;
        if (next == -1)
            last = -1;
        T value;
#if NET5_0_OR_GREATER
        Unsafe.SkipInit(out value);
#else
            value = default;
#endif
        slot = (value, firstUnused);
        firstUnused = old;
        return true;
    }

    public struct Enumerator
    {
        private readonly (T Value, int Next)[]? queue;
        private int current;

        public bool Has
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => queue is not null;
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Debug.Assert(queue is not null);
                return queue[current].Value;
            }
        }

        public int CurrentIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Debug.Assert(queue is not null);
                return current;
            }
        }

        public Enumerator((T Value, int Next)[]? queue, int start)
        {
            this.queue = queue;
            current = start;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Next()
        {
            Debug.Assert(queue is not null);
            current = queue[current].Next;
            return current != -1;
        }
    }
}
