using System;
using System.Collections.Generic;

namespace Enderlook.StateMachine;

/// <summary>
/// Represent parameters that can be passed to a <see cref="StateMachine{TState, TEvent, TRecipient}"/>.
/// </summary>
public interface IParameter
{
    internal int Store<TFirst>(ref SlotsQueue<ParameterSlot> parameterIndexes, Dictionary<Type, ParameterSlots> parameters);

    internal int Count { get; }
}
