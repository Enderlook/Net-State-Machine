using System;
using System.Collections.Generic;

namespace Enderlook.StateMachine
{
    /// <summary>
    /// Builder of an state machine.
    /// </summary>
    /// <typeparam name="TState">Type that determines states.</typeparam>
    /// <typeparam name="TEvent">Type that determines events.</typeparam>
    /// <typeparam name="TParameter">Type that determines common ground for parameters.</typeparam>
    public sealed class StateMachineBuilder<TState, TEvent, TParameter>
    {
        private Dictionary<TState, StateBuilder<TState, TEvent, TParameter>> states = new Dictionary<TState, StateBuilder<TState, TEvent, TParameter>>();
        private bool hasInitialState;
        private TState initialState;

        /// <summary>
        /// Add a new state.
        /// </summary>
        /// <param name="state">State to add.</param>
        /// <returns>State builder.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="state"/> was already registered.</exception>
        public StateBuilder<TState, TEvent, TParameter> In(TState state)
        {
            if (states.ContainsKey(state))
                throw new ArgumentException($"The state {state} was already registered.");

            StateBuilder<TState, TEvent, TParameter> builder = new StateBuilder<TState, TEvent, TParameter>(this, state);
            states.Add(state, builder);
            return builder;
        }

        /// <summary>
        /// Determines the initial state of the state machine.
        /// </summary>
        /// <param name="state">Initial state.</param>
        /// <returns><see cref="this"/>.</returns>
        /// <exception cref="InvalidOperationException">Throw when the initial state was already registered.</exception>
        public StateMachineBuilder<TState, TEvent, TParameter> SetInitialState(TState state)
        {
            if (hasInitialState)
                throw new InvalidOperationException("Already has a registered initial state.");
            hasInitialState = true;
            initialState = state;
            return this;
        }

        /// <summary>
        /// Convert the builder into an immutable state machinee.
        /// </summary>
        /// <returns>Immutable state machine.</returns>
        /// <exception cref="InvalidOperationException">Thrown when there is no registered initial state.<br/>
        /// Or when there are no registered states.<br/>
        /// Or when a transition refers to a non-registered state.</exception>
        public StateMachine<TState, TEvent, TParameter> Build()
        {
            if (!hasInitialState)
                throw new InvalidOperationException("The state machine builder doesn't have registered an initial state.");

            if (this.states.Count == 0)
                throw new InvalidOperationException("The state machine builder doesn't have registered any state.");

            Dictionary<TState, int> statesMap = new Dictionary<TState, int>();
            int i = 0;
            foreach (KeyValuePair<TState, StateBuilder<TState, TEvent, TParameter>> kv in this.states)
                statesMap.Add(kv.Key, i++);

            List<State<TState, TEvent>> states = new List<State<TState, TEvent>>();
            ListSlot<Transition<TState, TEvent>> transitions = new ListSlot<Transition<TState, TEvent>>(new List<Transition<TState, TEvent>>());

            // TODO: Use deconstruction pattern when upload to .Net Standard 2.1
            foreach (KeyValuePair<TState, StateBuilder<TState, TEvent, TParameter>> kv in this.states)
                states.Add(kv.Value.ToState(kv.Key, transitions, statesMap));

            return new StateMachine<TState, TEvent, TParameter>(initialState.TryGetStateIndex(statesMap), states, transitions.Extract());
        }
    }
}