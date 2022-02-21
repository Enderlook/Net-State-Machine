using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

/// <summary>
/// Represent a parameter than can be passed to callbacks of an <see cref="StateMachine{TState, TEvent, TRecipient}"/>.
/// </summary>
/// <typeparam name="T">Type of parameter.</typeparam>
public readonly struct Parameter<T> : IParameter
{
    private readonly T parameter;

    int IParameter.Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => 1;
    }

    /// <summary>
    /// Construct a parameter.
    /// </summary>
    /// <param name="parameter">Parameter to store.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Parameter(T parameter) => this.parameter = parameter;

    /// <summary>
    /// Chains an additional parameter to pass.
    /// </summary>
    /// <typeparam name="V">Type of the new parameter to add.</typeparam>
    /// <param name="parameter">Additional parameter to add.</param>
    /// <returns>Chained parameters.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Parameter<V, Parameter<T>> With<V>(V parameter)
        => new(parameter, this);

    int IParameter.Store<TFirst>(ref SlotsQueue<ParameterSlot> parameterIndexes, Dictionary<Type, ParameterSlots> parameters)
    {
        Debug.Assert(typeof(TFirst) == typeof(Yes) || typeof(TFirst) == typeof(No));
        if (!parameters.TryGetValue(typeof(T), out ParameterSlots? container))
            parameters.Add(typeof(T), container = new ParameterSlots<T>());
        Debug.Assert(container is ParameterSlots<T>);
        int index = Unsafe.As<ParameterSlots<T>>(container).Store(parameter);
        return parameterIndexes.StoreLast(new(container, index), typeof(TFirst) == typeof(No));
    }
}
