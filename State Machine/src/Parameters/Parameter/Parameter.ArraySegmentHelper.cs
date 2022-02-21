using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

public readonly partial struct Parameter
{
    private sealed class ArraySegmentHelper<T> : Helper
    {
        public static readonly ArraySegmentHelper<T> Singlenton = new();

        internal override int Store<TFirst>(ref SlotsQueue<ParameterSlot> parameterIndexes, Dictionary<Type, ParameterSlots> parameters, in Parameter parameter)
        {
            Debug.Assert(typeof(TFirst) == typeof(Yes) || typeof(TFirst) == typeof(No));
            if (!parameters.TryGetValue(typeof(ArraySegment<T>), out ParameterSlots? container))
                parameters.Add(typeof(ArraySegment<T>), container = new ParameterSlots<T>());
            Debug.Assert(container is ParameterSlots<ArraySegment<T>>);
            Debug.Assert(parameter.@object is T[]);
            ArraySegment<T> arraySegment = new ArraySegment<T>(Unsafe.As<T[]>(parameter.@object), parameter.union.ArraySegment.Offset, parameter.union.ArraySegment.Count);
            int index = Unsafe.As<ParameterSlots<ArraySegment<T>>>(container).Store(arraySegment);
            return parameterIndexes.StoreLast(new(container, index), typeof(TFirst) == typeof(No));
        }
    }
}