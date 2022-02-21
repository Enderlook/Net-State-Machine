using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

/// <summary>
/// Represent a set of parameters than can be passed to callbacks of an <see cref="StateMachine{TState, TEvent, TRecipient}"/>.<br/>
/// The chain behaviour of this type allow to avoid allocations when passing multiple parameters.
/// </summary>
/// <typeparam name="T">Type of parameter.</typeparam>
/// <typeparam name="U">Type of chained parameters.</typeparam>
public readonly struct Parameter<T, U> : IParameter
    where U : struct
{
    private readonly T parameter;
    private readonly U previous;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Parameter(T parameter, U previous)
    {
        this.parameter = parameter;
        this.previous = previous;
    }

    int IParameter.Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Debug.Assert(typeof(IParameter).IsAssignableFrom(typeof(U)));
            return 1 + ((IParameter)previous).Count;
        }
    }

    /// <summary>
    /// Chains an additional parameter to pass.
    /// </summary>
    /// <typeparam name="V">Type of the new parameter to add.</typeparam>
    /// <param name="parameter">Additional parameter to add.</param>
    /// <returns>Chained parameters.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Parameter<V, Parameter<T, U>> With<V>(V parameter)
        => new(parameter, this);

    int IParameter.Store<TFirst>(ref SlotsQueue<ParameterSlot> parameterIndexes, Dictionary<Type, ParameterSlots> parameters)
    {
        Debug.Assert(typeof(TFirst) == typeof(Yes) || typeof(TFirst) == typeof(No));
        Debug.Assert(typeof(IParameter).IsAssignableFrom(typeof(U)));
        parameterIndexes.EnsureCapacity(1 + ((IParameter)previous).Count);
        if (!parameters.TryGetValue(typeof(T), out ParameterSlots? container))
            parameters.Add(typeof(T), container = new ParameterSlots<T>());
        Debug.Assert(container is ParameterSlots<T>);
        int index = Unsafe.As<ParameterSlots<T>>(container).Store(parameter);
        int start = parameterIndexes.StoreLast(new(container, index), typeof(TFirst) == typeof(No));
        ((IParameter)previous).Store<No>(ref parameterIndexes, parameters);
        return start;
    }
}