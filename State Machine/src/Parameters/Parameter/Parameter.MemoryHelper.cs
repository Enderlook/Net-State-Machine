using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

public readonly partial struct Parameter
{
    private sealed class MemoryHelper<T, TIsReadOnly> : Helper
    {
        public static readonly MemoryHelper<T, TIsReadOnly> Singlenton = new();

        internal override int Store<TFirst>(ref SlotsQueue<ParameterSlot> parameterIndexes, Dictionary<Type, ParameterSlots> parameters, in Parameter parameter)
        {
            Debug.Assert(typeof(TFirst) == typeof(Yes) || typeof(TFirst) == typeof(No));
            Debug.Assert(typeof(TIsReadOnly) == typeof(Yes) || typeof(TIsReadOnly) == typeof(No));
            Type key = typeof(TIsReadOnly) == typeof(Yes) ? typeof(ReadOnlyMemory<T>) : typeof(Memory<T>);
            if (!parameters.TryGetValue(key, out ParameterSlots? container))
                parameters.Add(key, container = new ParameterSlots<T>());
            Debug.Assert(typeof(TIsReadOnly) == typeof(Yes) ? container is ParameterSlots<ReadOnlyMemory<T>> : container is ParameterSlots<Memory<T>>);
            int index;
            if (typeof(TIsReadOnly) == typeof(Yes))
                index = Unsafe.As<ParameterSlots<ReadOnlyMemory<T>>>(container).Store(FakeMemory.To<T>(parameter));
            else
                index = Unsafe.As<ParameterSlots<Memory<T>>>(container).Store(FakeMemory.To<T>(parameter));
            return parameterIndexes.StoreLast(new(container, index), typeof(TFirst) == typeof(No));
        }
    }
}