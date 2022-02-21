using Enderlook.Pools;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

/// <summary>
/// Stores a parameter that can be passed to a state machine.
/// </summary>
public readonly partial struct Parameter
{
    private readonly Helper helper;
    private readonly object? @object;
    private readonly Union union;

    private Parameter(object? @object, Union union, Helper helper)
    {
        this.@object = @object;
        this.union = union;
        this.helper = helper;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int Store<TFirst>(ref SlotsQueue<ParameterSlot> parameterIndexes, Dictionary<Type, ParameterSlots> parameters)
        => helper.Store<TFirst>(ref parameterIndexes, parameters, this);

    /// <summary>
    /// Construct the parameter from an <see cref="ArraySegment{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type of element.</typeparam>
    /// <param name="value">Value to use as parameter.</param>
    /// <returns>A parameter than contains the specifed value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parameter From<T>(ArraySegment<T> value) => new(value.Array, new(value.Offset, value.Count), ArraySegmentHelper<T>.Singlenton);

    /// <summary>
    /// Construct the parameter from an <see cref="Memory{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type of element.</typeparam>
    /// <param name="value">Value to use as parameter.</param>
    /// <returns>A parameter than contains the specifed value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parameter From<T>(Memory<T> value) => FakeMemory.From(value);

    /// <summary>
    /// Construct the parameter from an <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type of element.</typeparam>
    /// <param name="value">Value to use as parameter.</param>
    /// <returns>A parameter than contains the specifed value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parameter From<T>(ReadOnlyMemory<T> value) => FakeMemory.From(value);

    /// <summary>
    /// Construct the parameter from a value.<br/>
    /// For better perfomance, it's recommended to use the overloards of this method for concrete types such as: <see cref="From{T}(T?)"/>, <see cref="From{T}(ArraySegment{T})"/>, <see cref="From{T}(Memory{T})"/> and <see cref="From{T}(ReadOnlyMemory{T})"/>.
    /// </summary>
    /// <typeparam name="T">Type of parameter.</typeparam>
    /// <param name="value">Value to use as parameter.</param>
    /// <returns>A parameter than contains the specifed value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parameter From<T>(T value)
    {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() && Unsafe.SizeOf<T>() <= Unsafe.SizeOf<Union.Container>())
            return new(null, new(Unsafe.As<T, Union.Container>(ref Unsafe.AsRef(value))), DefaultHelper<T>.Singlenton);
        if (!typeof(T).IsValueType)
            return new(value, default, DefaultHelper<T>.Singlenton);
        StrongBox<T> box = ObjectPool<StrongBox<T>>.Shared.Rent();
        box.Value = value;
        return new(box, default, DefaultHelper<T>.Singlenton);
#else
        if (!typeof(T).IsValueType)
            return new(value, default, DefaultHelper<T>.Singlenton);
        if (typeof(T) == typeof(byte) ||
            typeof(T) == typeof(sbyte) ||
            typeof(T) == typeof(char) ||
            typeof(T) == typeof(bool) ||
            typeof(T) == typeof(short) ||
            typeof(T) == typeof(ushort) ||
            typeof(T) == typeof(int) ||
            typeof(T) == typeof(uint) ||
            typeof(T) == typeof(long) ||
            typeof(T) == typeof(ulong) ||
            typeof(T) == typeof(float) ||
            typeof(T) == typeof(double) ||
            typeof(T) == typeof(decimal) ||
            typeof(T) == typeof(DateTime) ||
            typeof(T) == typeof(DateTimeOffset) ||
            typeof(T) == typeof(Guid))
            return new(null, new(Unsafe.As<T, Union.Container>(ref Unsafe.AsRef(value))), DefaultHelper<T>.Singlenton);
        StrongBox<T> box = ObjectPool<StrongBox<T>>.Shared.Rent();
        box.Value = value;
        return new(box, default, DefaultHelper<T>.Singlenton);
#endif
    }

    /// <summary>
    /// Construct the parameter from a nullable value.
    /// </summary>
    /// <typeparam name="T">Type of parameter.</typeparam>
    /// <param name="value">Value to use as parameter.</param>
    /// <returns>A parameter than contains the specifed value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parameter From<T>(T? value)
        where T : struct
    {
        if (value is T value_)
        {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() && Unsafe.SizeOf<T>() <= Unsafe.SizeOf<Union.Container>())
                return new(null, new(Unsafe.As<T, Union.Container>(ref Unsafe.AsRef(value_))), SimpleNullableHelper<T, No>.Singlenton);
            return new(value, default, SimpleNullableHelper<T, No>.Singlenton);
#else
        if (typeof(T) == typeof(byte) ||
            typeof(T) == typeof(sbyte) ||
            typeof(T) == typeof(char) ||
            typeof(T) == typeof(bool) ||
            typeof(T) == typeof(short) ||
            typeof(T) == typeof(ushort) ||
            typeof(T) == typeof(int) ||
            typeof(T) == typeof(uint) ||
            typeof(T) == typeof(long) ||
            typeof(T) == typeof(ulong) ||
            typeof(T) == typeof(float) ||
            typeof(T) == typeof(double) ||
            typeof(T) == typeof(decimal) ||
            typeof(T) == typeof(DateTime) ||
            typeof(T) == typeof(DateTimeOffset) ||
            typeof(T) == typeof(TimeSpan) ||
            typeof(T) == typeof(Guid))
            return new(null, new(Unsafe.As<T, Union.Container>(ref Unsafe.AsRef(value_))), SimpleNullableHelper<T, No>.Singlenton);
            return new(value, default, SimpleNullableHelper<T, No>.Singlenton);
#endif
        }
        else
            return new(null, default, SimpleNullableHelper<T, Yes>.Singlenton);
    }
}