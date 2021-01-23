using System;
using System.Collections.Generic;

namespace Enderlook.StateMachine
{
    internal static class Helper
    {
        /// <summary>
        /// Extract the index of an state.
        /// </summary>
        /// <param name="state">State to query.</param>
        /// <param name="statesMap">Possible states.</param>
        /// <returns>Index of the given state.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the state <paramref name="state"/> is not registered.</exception>
        internal static int TryGetStateIndex<TState>(this TState state, Dictionary<TState, int> statesMap)
        {
            if (statesMap.TryGetValue(state, out int index))
                return index;
            throw new InvalidOperationException("Transition has a goto to an unregistered state.");
        }
    }
}