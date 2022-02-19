using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

/// <summary>
/// Represent an slice of data.
/// </summary>
/// <typeparam name="T">Type of data.</typeparam>
public readonly struct ReadOnlySlice<T> : IReadOnlyList<T>
{
    private readonly T[] array;
    private readonly int start;
    private readonly int length;

    /// <summary>
    /// Get the element specified at the index.
    /// </summary>
    /// <param name="index">Index to retrieve.</param>
    /// <returns>Element at the index <paramref name="index"/>.</returns>
    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (index < 0 || index >= length) ThrowHelper.ThrowArgumentOutOfRangeException_Index();
            return array[start + index];
        }
    }

    /// <inheritdoc cref="IReadOnlyCollection{T}.Count"/>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => length;
    }

    /// <summary>
    /// Get an <see cref="ReadOnlyMemory{T}"/> of this slice.
    /// </summary>
    public ReadOnlyMemory<T> Memory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => array.AsMemory(start, length);
    }

    /// <summary>
    /// Get an <see cref="ReadOnlySpan{T}"/> of this slice.
    /// </summary>
    public ReadOnlySpan<T> Span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => array.AsSpan(start, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySlice(T[] array, int start, int length)
    {
        this.array = array;
        this.start = start;
        this.length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySlice<T> WithoutFirst()
    {
        Debug.Assert(length > 0);
        if (length == 1)
            return default;
        return new ReadOnlySlice<T>(array, start + 1, length - 1);
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public Enumerator GetEnumerator() => new Enumerator(this);

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Enumerator of <see cref="ReadOnlySlice{T}"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly ReadOnlySlice<T> slice;
        private int index;

        internal Enumerator(ReadOnlySlice<T> slice)
        {
            this.slice = slice;
            index = -1;
        }

        /// <inheritdoc cref="IEnumerator{T}.Current"/>
        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => slice[index];
        }

        /// <inheritdoc cref="IEnumerator.Current"/>
        object IEnumerator.Current => Current;

        /// <inheritdoc cref="IDisposable.Dispose"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IDisposable.Dispose() { }

        /// <inheritdoc cref="IEnumerator.MoveNext"/>
        public bool MoveNext()
        {
            index++;
            return index < slice.length;
        }

        /// <inheritdoc cref="IEnumerator.Reset"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => index = -1;
    }
}
