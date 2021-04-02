using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

        /// <summary>
        /// Combine two delegates.
        /// </summary>
        /// <typeparam name="TParameter">Type of parameters used by <paramref name="actionWithParameter"/>.</typeparam>
        /// <param name="action">First action.</param>
        /// <param name="actionWithParameter">Second action.</param>
        /// <returns>Combination of both actions.</returns>
        internal static Delegate Combine<TParameter>(ref Action action, ref Action<TParameter> actionWithParameter)
        {
            if (action is null)
                return actionWithParameter;
            if (actionWithParameter is null)
                return action;
            Action a = action;
            Action<TParameter> b = actionWithParameter;
            actionWithParameter = new Action<TParameter>((parameter) =>
            {
                a();
                b(parameter);
            });
            action = null;
            return actionWithParameter;
        }

        /// <summary>
        /// Executes the specified delegate.
        /// </summary>
        /// <typeparam name="TParameter">Type of the parameter.</typeparam>
        /// <param name="delegate">Delegate to execute.</param>
        /// <param name="parameter">Parameter to pass to delegate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExecuteVoid<TParameter>(Delegate @delegate, TParameter parameter)
        {
            if (@delegate is null)
                return;
            switch (@delegate)
            {
                case Action action:
                    action();
                    break;
                case Action<TParameter> action:
                    action(parameter);
                    break;
            }
        }
    }
}