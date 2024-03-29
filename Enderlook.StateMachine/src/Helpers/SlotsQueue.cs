﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        get
        {
#if NET5_0_OR_GREATER
            Debug.Assert(queue.Length > index);
            return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(queue), index).Value;
#else
            return queue[index].Value;
#endif
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SlotsQueue(int initialSize)
    {
        queue = initialSize == 0 ? Array.Empty<(T Value, int Next)>() : new (T Value, int Next)[initialSize];
        first = -1;
        last = -1;
        firstUnused = -1;
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
        if (slot == first)
            first = -1;
        if (slot == last)
            last = -1;

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
#if NET5_0_OR_GREATER
            Debug.Assert(queue.Length > slot);
            Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(queue), slot).Next = firstUnused;
#else
            queue[slot].Next = firstUnused;
#endif
        }
        else
#endif
        {
#if NET5_0_OR_GREATER
            Debug.Assert(queue.Length > slot);
            Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(queue), slot) = (default!, firstUnused);
#else
            queue[slot] = (default!, firstUnused);
#endif
        }

        firstUnused = slot;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveFrom(int start)
    {
        int previous;
        int current = start;
        int unused = firstUnused;
#if NET5_0_OR_GREATER
        ref (T Value, int Next) queue_ = ref MemoryMarshal.GetArrayDataReference(queue);
#else
        ref (T Value, int Next) queue_ = ref queue[0];
#endif
        do
        {
            int next;
            Debug.Assert(queue.Length > current);
            ref (T Value, int Next) slot = ref Unsafe.Add(ref queue_, current);
            next = slot.Next;
            if (typeof(T) == typeof(ParameterSlot))
                Unsafe.As<T, ParameterSlot>(ref slot.Value).Remove();
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                slot.Next = firstUnused;
            else
#endif
                slot = (default!, unused);
            unused = current;
            previous = current;
            current = next;
        } while (current != -1);

        firstUnused = unused;
        if (start == first)
            first = -1;
        if (previous == last)
            last = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] // Required for constant propagation of parameter
    public int StoreLast(T element, bool connectToPrevious = true)
    {
        int slot = firstUnused;
        if (slot != -1)
        {
#if NET5_0_OR_GREATER
            Debug.Assert(queue.Length > slot);
            firstUnused = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(queue), slot).Next;
#else
            firstUnused = queue[slot].Next;
#endif
        }
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

#if NET5_0_OR_GREATER
        Debug.Assert(queue.Length > slot);
        Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(queue), slot) = (element, -1);
#else
        queue[slot] = (element, -1);
#endif
        int last_ = last;
        if (connectToPrevious && last_ != -1)
        {
#if NET5_0_OR_GREATER
            Debug.Assert(queue.Length > last_);
            Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(queue), last_).Next = slot;
#else
            queue[last_].Next = slot;
#endif
        }
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

#if NET5_0_OR_GREATER
        Debug.Assert(queue.Length > old);
        ref (T Value, int Next) slot = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(queue), old);
#else
        ref (T Value, int Next) slot = ref queue[old];
#endif
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
