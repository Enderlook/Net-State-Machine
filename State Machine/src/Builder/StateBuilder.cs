using System;
using System.Collections.Generic;

namespace Enderlook.StateMachine
{
    /// <summary>
    /// Builder of a concrete state.
    /// </summary>
    /// <typeparam name="TState">Type that determines states.</typeparam>
    /// <typeparam name="TEvent">Type that determines events.</typeparam>
    /// <typeparam name="TParameter">Type that determines common ground for parameters.</typeparam>
    public sealed class StateBuilder<TState, TEvent, TParameter>
    {
        private Action onEntry;
        private Action<TParameter> onEntryWithParameter;
        private Action onExit;
        private Action<TParameter> onExitWithParameter;
        private Action onUpdate;
        private Action<TParameter> onUpdateWithParameter;
        internal TState State { get; }

        private Dictionary<TEvent, TransitionBuilder<TState, TEvent, TParameter>> transitions = new Dictionary<TEvent, TransitionBuilder<TState, TEvent, TParameter>>();
        private StateMachineBuilder<TState, TEvent, TParameter> parent;

        internal StateBuilder(StateMachineBuilder<TState, TEvent, TParameter> parent, TState state)
        {
            this.parent = parent;
            State = state;
        }

        /// <inheritdoc cref="StateMachineBuilder{TState, TEvent, TParameter}.In(TState)"/>
        public StateBuilder<TState, TEvent, TParameter> In(TState state) => parent.In(state);

        /// <inheritdoc cref="StateMachineBuilder{TState, TEvent, TParameter}.Build"/>
        public StateMachine<TState, TEvent, TParameter> Build() => parent.Build();

        /// <summary>
        /// Determines an action to execute on entry to this state.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        /// <returns><see cref="this"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
        public StateBuilder<TState, TEvent, TParameter> OnEntry(Action action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            onEntry += action;
            return this;
        }

        /// <inheritdoc cref="OnEntry(Action)"/>
        public StateBuilder<TState, TEvent, TParameter> OnEntry(Action<TParameter> action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            onEntryWithParameter += action;
            return this;
        }

        /// <summary>
        /// Determines an action to execute on exit of this state.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        /// <returns><see cref="this"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
        public StateBuilder<TState, TEvent, TParameter> OnExit(Action action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            onExit += action;
            return this;
        }

        /// <inheritdoc cref="OnExit(Action)"/>
        public StateBuilder<TState, TEvent, TParameter> OnExit(Action<TParameter> action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            onExitWithParameter += action;
            return this;
        }

        /// <summary>
        /// Determines an action to execute on update while in this state.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        /// <returns><see cref="this"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
        public StateBuilder<TState, TEvent, TParameter> OnUpdate(Action action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            onUpdate += action;
            return this;
        }

        /// <inheritdoc cref="OnUpdate(Action)"/>
        public StateBuilder<TState, TEvent, TParameter> OnUpdate(Action<TParameter> action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            onUpdateWithParameter += action;
            return this;
        }

        /// <summary>
        /// Add a behaviour that is executed on an event.
        /// </summary>
        /// <param name="event">Raised event.</param>
        /// <returns>Transition builder.</returns>
        /// <exception cref="ArgumentException">Thrown when this state already has registered <paramref name="event"/>.</exception>
        public MasterTransitionBuilder<TState, TEvent, TParameter> On(TEvent @event)
        {
            if (transitions.ContainsKey(@event))
                throw new ArgumentException($"The event {@event} was already registered for this state.");

            MasterTransitionBuilder<TState, TEvent, TParameter> builder = new MasterTransitionBuilder<TState, TEvent, TParameter>(this);
            transitions.Add(@event, builder);
            return builder;
        }

        /// <summary>
        /// Ignores an event.
        /// </summary>
        /// <param name="event">Event to ignore.</param>
        /// <returns><see cref="this"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when this state already has registered <paramref name="event"/>.</exception>
        public StateBuilder<TState, TEvent, TParameter> Ignore(TEvent @event)
        {
            if (transitions.ContainsKey(@event))
                throw new ArgumentException($"The event {@event} was already registered for this state.");

            transitions.Add(@event, null);
            return this;
        }

        internal State<TState, TEvent> ToState(TState state, ListSlot<Transition<TState, TEvent>> transitions, Dictionary<TState, int> statesMap)
        {
            Dictionary<TEvent, int> trans = new Dictionary<TEvent, int>();

            // TODO: Use deconstruction pattern when upload to .Net Standard 2.1
            foreach (KeyValuePair<TEvent, TransitionBuilder<TState, TEvent, TParameter>> kv in this.transitions)
            {
                int slot = transitions.Reserve();
                trans.Add(kv.Key, slot);
                if (kv.Value is null)
                    transitions.Store(new Transition<TState, TEvent>(-1, null, (0, 0)), slot);
                else
                    transitions.Store(kv.Value.ToTransition(transitions, statesMap, State), slot);
            }

            return new State<TState, TEvent>(
                state,
                Helper.Combine(ref onEntry, ref onEntryWithParameter),
                Helper.Combine(ref onExit, ref onExitWithParameter),
                Helper.Combine(ref onUpdate, ref onUpdateWithParameter),
                trans
            );
        }
    }
}