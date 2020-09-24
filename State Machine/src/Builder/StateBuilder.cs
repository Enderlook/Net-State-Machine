using System;
using System.Collections.Generic;

namespace Enderlook.StateMachine
{
    /// <summary>
    /// Builder of a concrete state.
    /// </summary>
    /// <typeparam name="TState">Type that determines states.</typeparam>
    /// <typeparam name="TEvent">Type that determines events.</typeparam>
    public class StateBuilder<TState, TEvent>
        where TState : IComparable
        where TEvent : IComparable
    {
        private Delegate onEntry;
        private Delegate onExit;
        private Delegate onUpdate;
        internal TState State { get; }

        private Dictionary<TEvent, TransitionBuilder<TState, TEvent>> transitions = new Dictionary<TEvent, TransitionBuilder<TState, TEvent>>();
        private StateMachineBuilder<TState, TEvent> parent;

        internal StateBuilder(StateMachineBuilder<TState, TEvent> parent, TState state)
        {
            this.parent = parent;
            State = state;
        }

        /// <inheritdoc cref="StateMachineBuilder{TState, TEvent}.In(TState)"/>
        public StateBuilder<TState, TEvent> In(TState state) => parent.In(state);

        /// <inheritdoc cref="StateMachineBuilder{TState, TEvent}.Build"/>
        public StateMachine<TState, TEvent> Build() => parent.Build();

        /// <summary>
        /// Determines an action to execute on entry to this state.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        /// <returns><see cref="this"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when this state already has a registered entry action.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
        private StateBuilder<TState, TEvent> ExecuteOnEntry(Delegate action)
        {
            if (!(onEntry is null))
                throw new InvalidOperationException("Already has a registered entry action");
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            onEntry = action;
            return this;
        }

        /// <inheritdoc cref="ExecuteOnEntry(Delegate)"/>
        public StateBuilder<TState, TEvent> ExecuteOnEntry(Action action)
            => ExecuteOnEntry((Delegate)action);

        /// <inheritdoc cref="ExecuteOnEntry(Delegate)"/>
        public StateBuilder<TState, TEvent> ExecuteOnEntry(Action<object> action)
            => ExecuteOnEntry((Delegate)action);

        /// <summary>
        /// Determines an action to execute on exit of this state.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        /// <returns><see cref="this"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when this state already has a registered exit action.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
        private StateBuilder<TState, TEvent> ExecuteOnExit(Delegate action)
        {
            if (!(onExit is null))
                throw new InvalidOperationException("Already has a registered exit action");
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            onExit = action;
            return this;
        }

        /// <inheritdoc cref="ExecuteOnExit(Delegate)"/>
        public StateBuilder<TState, TEvent> ExecuteOnExit(Action action)
            => ExecuteOnExit((Delegate)action);

        /// <inheritdoc cref="ExecuteOnExit(Delegate)"/>
        public StateBuilder<TState, TEvent> ExecuteOnExit(Action<object> action)
            => ExecuteOnExit((Delegate)action);

        /// <summary>
        /// Determines an action to execute on update while in this state.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        /// <returns><see cref="this"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when this state already has a registered update action.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
        private StateBuilder<TState, TEvent> ExecuteOnUpdate(Delegate action)
        {
            if (!(onUpdate is null))
                throw new InvalidOperationException("Already has a registered entry action");
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            onUpdate = action;
            return this;
        }

        /// <inheritdoc cref="ExecuteOnUpdate(Delegate)"/>
        public StateBuilder<TState, TEvent> ExecuteOnUpdate(Action action)
            => ExecuteOnUpdate((Delegate)action);

        /// <inheritdoc cref="ExecuteOnUpdate(Delegate)"/>
        public StateBuilder<TState, TEvent> ExecuteOnUpdate(Action<object> action)
            => ExecuteOnUpdate((Delegate)action);

        /// <summary>
        /// Add a behaviour that is executed on an event.
        /// </summary>
        /// <param name="event">Raised event.</param>
        /// <returns>Transition builder.</returns>
        /// <exception cref="ArgumentException">Thrown when this state already has registered <paramref name="event"/>.</exception>
        public MasterTransitionBuilder<TState, TEvent> On(TEvent @event)
        {
            if (transitions.ContainsKey(@event))
                throw new ArgumentException($"The event {transitions} was already registered for this state.");

            MasterTransitionBuilder<TState, TEvent> builder = new MasterTransitionBuilder<TState, TEvent>(this);
            transitions.Add(@event, builder);
            return builder;
        }

        /// <summary>
        /// Ignores an event.
        /// </summary>
        /// <param name="event">Event to ignore.</param>
        /// <returns><see cref="this"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when this state already has registered <paramref name="event"/>.</exception>
        public StateBuilder<TState, TEvent> Ignore(TEvent @event)
        {
            if (transitions.ContainsKey(@event))
                throw new ArgumentException($"The event {transitions} was already registered for this state.");

            transitions.Add(@event, null);
            return this;
        }

        internal State<TState, TEvent> ToState(TState state, ListSlot<Transition<TState, TEvent>> transitions, Dictionary<TState, int> statesMap)
        {
            Dictionary<TEvent, int> trans = new Dictionary<TEvent, int>();

            // TODO: Use deconstruction pattern when upload to .Net Standard 2.1
            foreach (KeyValuePair<TEvent, TransitionBuilder<TState, TEvent>> kv in this.transitions)
            {
                int slot = transitions.Reserve();
                trans.Add(kv.Key, slot);
                if (kv.Value is null)
                    transitions.Store(new Transition<TState, TEvent>(-1, null, (0,0)), slot);
                else
                    transitions.Store(kv.Value.ToTransition(transitions, statesMap), slot);
            }

            return new State<TState, TEvent>(state, onEntry, onExit, onUpdate, trans);
        }
    }
}