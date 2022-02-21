using Enderlook.Pools;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

public readonly partial struct Parameter
{
    private abstract class Helper
    {
        internal abstract int Store<TFirst>(ref SlotsQueue<ParameterSlot> parameterIndexes, Dictionary<Type, ParameterSlots> parameters, in Parameter parameter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetSimpleValue<T>(in Parameter parameter)
        {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() && Unsafe.SizeOf<T>() <= Unsafe.SizeOf<Union.Container>())
                return Unsafe.As<Union.Container, T>(ref Unsafe.AsRef(parameter.union.Storage));
            if (!typeof(T).IsValueType)
                return Unsafe.As<object?, T>(ref Unsafe.AsRef(parameter.@object));
            Debug.Assert(parameter.@object is StrongBox<T>);
            StrongBox<T> box = Unsafe.As<StrongBox<T>>(parameter.@object);
            T value = box.Value!;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                box.Value = default!;
            ObjectPool<StrongBox<T>>.Shared.Return(box);
            return value;
#else
            if (!typeof(T).IsValueType)
                return Unsafe.As<object?, T>(ref Unsafe.AsRef(parameter.@object));
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
                return Unsafe.As<Union.Container, T>(ref Unsafe.AsRef(parameter.union.Storage));
            Debug.Assert(parameter.@object is StrongBox<T>);
            StrongBox<T> box = Unsafe.As<StrongBox<T>>(parameter.@object);
            T value = box.Value!;
            ObjectPool<StrongBox<T>>.Shared.Return(box);
            return value;
#endif
        }
    }
}
