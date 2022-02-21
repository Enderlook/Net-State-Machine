using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

public readonly partial struct Parameter
{
    private sealed class DefaultHelper<T> : Helper
    {
        public static readonly DefaultHelper<T> Singlenton = new();

        internal override int Store<TFirst>(ref SlotsQueue<ParameterSlot> parameterIndexes, Dictionary<Type, ParameterSlots> parameters, in Parameter parameter)
        {
            Debug.Assert(typeof(TFirst) == typeof(Yes) || typeof(TFirst) == typeof(No));
            if (!parameters.TryGetValue(typeof(T), out ParameterSlots? container))
                parameters.Add(typeof(T), container = new ParameterSlots<T>());
            Debug.Assert(container is ParameterSlots<T>);
            int index = Unsafe.As<ParameterSlots<T>>(container).Store(GetSimpleValue<T>(parameter));
            return parameterIndexes.StoreLast(new(container, index), typeof(TFirst) == typeof(No));
        }
    }
}