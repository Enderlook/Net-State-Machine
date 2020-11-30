using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Enderlook.StateMachine
{
    /// <summary>
    /// Representation of an state machine
    /// </summary>
    /// <typeparam name="TState">Type that determines states.</typeparam>
    /// <typeparam name="TEvent">Type that determines events.</typeparam>
    public class StateMachine<TState, TEvent>
        where TState : IComparable
        where TEvent : IComparable
    {
        private int currentState;
        private List<State<TState, TEvent>> states;
        private List<Transition<TState, TEvent>> transitions;
        private List<Transition<TState, TEvent>> transitionsToExecute = new List<Transition<TState, TEvent>>();

        /// <summary>
        /// Returns the current state of this state machine.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="Start()"/> nor <see cref="Start(object)"/> has been called yet.</exception>
        public TState State => currentState < 0 ? throw new InvalidOperationException("State machine is already started.") : states[currentState].state;

        /// <summary>
        /// Creates the builder of an state machine.
        /// </summary>
        /// <returns>Builder of the state machine.</returns>
        public static StateMachineBuilder<TState, TEvent> Builder()
            => new StateMachineBuilder<TState, TEvent>();

        internal StateMachine(int initialState, List<State<TState, TEvent>> states, List<Transition<TState, TEvent>> transitions)
        {
            currentState = -initialState - 1;
            this.states = states;
            this.transitions = transitions;
        }

        /// <inheritdoc cref="Start(object)"/>
        public void Start() => Start(null);

        /// <summary>
        /// Initializes the state machine.
        /// </summary>
        /// <param name="parameter">Parameter passed to the OnEntry delegate in the initial state (if any).</param>
        public void Start(object parameter)
        {
            if (currentState >= 0)
                throw new InvalidOperationException("State machine is already started.");
            currentState = -(currentState + 1);
            State<TState, TEvent> state = states[currentState];
            ExecuteStateEntry(state, parameter);
        }

        /// <inheritdoc cref="Fire(TEvent, object)"/>
        public void Fire(TEvent @event) => Fire(@event, null);

        /// <summary>
        /// Fires an event.
        /// </summary>
        /// <param name="event">Event to fire.</param>
        /// <param name="parameter">Parameter of the event.</param>
        public void Fire(TEvent @event, object parameter)
        {
            if (this.currentState < 0)
                throw new InvalidOperationException("State machine has not started.");

            State<TState, TEvent> currentState = states[this.currentState];
            if (currentState.transitions.TryGetValue(@event, out int transitionIndex))
            {
                Transition<TState, TEvent> transition = transitions[transitionIndex];
                Debug.Assert(transition.guard is null, "Master transition can't have guard.");
                (int from, int to) = transition.transitions;
                if (from == 0 && to == 0)
                {
                    ExecuteTransition(transition, parameter);
                    TryGoto(transition, currentState, parameter);
                }
                else
                {
                    for (int i = from; i < to; i++)
                    {
                        if (InspectSubTransition(i, currentState, parameter))
                        {
                            ExecuteTransition(transition, parameter);
                            return;
                        }
                    }
                    ExecuteTransition(transition, parameter);
                    TryGoto(transition, currentState, parameter);
                }
            }
            else
                throw new ArgumentException($"State {currentState.state} doesn't have any transition with event {@event}");
        }

        /// <inheritdoc cref="Update(object)"/>
        public void Update() => Update(null);

        /// <summary>
        /// Executes the update event of the current state if has any.
        /// </summary>
        /// <param name="parameter">Parameter of the event.</param>
        public void Update(object parameter)
        {
            if (currentState < 0)
                throw new InvalidOperationException("State machine has not started.");

            ExecuteVoid(states[currentState].onUpdate, parameter);
        }

        private bool InspectSubTransition(int subTransitionIndex, State<TState, TEvent> currentState, object parameter)
        {
            Transition<TState, TEvent> transition = transitions[subTransitionIndex];
            if (TryGuard(transition, parameter))
            {
                transitionsToExecute.Add(transition);

                (int from, int to) = transition.transitions;
                if (from == 0 && to == 0)
                    ExecuteTransitionQueue(parameter);
                else
                    for (int transitionIndex = from; transitionIndex < to; transitionIndex++)
                        if (InspectSubTransition(transitionIndex, currentState, parameter))
                            return true;

                TryGoto(transition, currentState, parameter);
                return true;
            }
            return false;
        }

        private void ExecuteTransitionQueue(object parameter)
        {
            for (int i = 0; i < transitionsToExecute.Count; i++)
                ExecuteTransition(transitionsToExecute[i], parameter);
            transitionsToExecute.Clear();
        }

        private bool TryGuard(Transition<TState, TEvent> transition, object parameter)
        {
            Delegate @delegate = transition.guard;
            if (@delegate is null)
                return true;
            switch (@delegate)
            {
                case Func<bool> action:
                    return action.Invoke();
                case Func<object, bool> action:
                    return action.Invoke(parameter);
            }
#if DEBUG
            Debug.Fail("Impossible State");
#endif
            return true;
        }

        private void TryGoto(Transition<TState, TEvent> transition, State<TState, TEvent> currentState, object parameter)
        {
            if (transition.Maintain)
                return;

            ExecuteStateExit(currentState, parameter);
            int @goto = transition.@goto;
            this.currentState = @goto;
            ExecuteStateEntry(states[@goto], parameter);
        }

        private void ExecuteStateEntry(State<TState, TEvent> state, object parameter)
            => ExecuteVoid(state.onEntry, parameter);

        private void ExecuteStateExit(State<TState, TEvent> state, object parameter)
            => ExecuteVoid(state.onExit, parameter);

        private void ExecuteTransition(Transition<TState, TEvent> transition, object parameter)
            => ExecuteVoid(transition.action, parameter);

        private void ExecuteVoid(Delegate @delegate, object parameter)
        {
            if (@delegate is null)
                return;
            switch (@delegate)
            {
                case Action action:
                    action.Invoke();
                    break;
                case Action<object> action:
                    action.Invoke(parameter);
                    break;
            }
        }
    }
}