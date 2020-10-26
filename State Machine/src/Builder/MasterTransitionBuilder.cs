using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Enderlook.StateMachine
{
    /// <summary>
    /// Builder of concrete master transitions.
    /// </summary>
    /// <typeparam name="TState">Type that determines states.</typeparam>
    /// <typeparam name="TEvent">Type that determines events.</typeparam>
    public class MasterTransitionBuilder<TState, TEvent> : TransitionBuilder<TState, TEvent>
        where TState : IComparable
        where TEvent : IComparable
    {
        private List<SlaveTransitionBuilder<TState, TEvent, MasterTransitionBuilder<TState, TEvent>>> slaves;
        private StateBuilder<TState, TEvent> parent;
        internal override TState SelfState => parent.State;

        internal MasterTransitionBuilder(StateBuilder<TState, TEvent> parent)
            => this.parent = parent;

        /// <inheritdoc cref="If(Delegate)"/>
        public SlaveTransitionBuilder<TState, TEvent, MasterTransitionBuilder<TState, TEvent>> If(Func<bool> guard)
            => If((Delegate)guard);

        /// <inheritdoc cref="If(Delegate)"/>
        public SlaveTransitionBuilder<TState, TEvent, MasterTransitionBuilder<TState, TEvent>> If(Func<object, bool> guard)
            => If((Delegate)guard);

        /// <summary>
        /// Add a sub transition with a condition.
        /// </summary>
        /// <param name="guard">Condition to execute transition.</param>
        /// <returns>Sub transition.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="guard"/> is <see langword="null"/>.</exception>
        private SlaveTransitionBuilder<TState, TEvent, MasterTransitionBuilder<TState, TEvent>> If(Delegate guard)
        {
            SlaveTransitionBuilder<TState, TEvent, MasterTransitionBuilder<TState, TEvent>> slave = new SlaveTransitionBuilder<TState, TEvent, MasterTransitionBuilder<TState, TEvent>>(guard, this);
            if (slaves is null)
                slaves = new List<SlaveTransitionBuilder<TState, TEvent, MasterTransitionBuilder<TState, TEvent>>>();
            slaves.Add(slave);
            return slave;
        }

        internal override Transition<TState, TEvent> ToTransition(ListSlot<Transition<TState, TEvent>> transitions, Dictionary<TState, int> statesMap)
        {
            if (slaves == null)
                return new Transition<TState, TEvent>(GetGoto(statesMap), action, (0, 0));
            (int from, int to) range = transitions.Reserve(slaves.Count);

            int i = range.from;
            foreach (SlaveTransitionBuilder<TState, TEvent, MasterTransitionBuilder<TState, TEvent>> slave in slaves)
            {
                transitions.Store(slave.ToTransition(transitions, statesMap), i);
                i++;
            }
            Debug.Assert(i == range.to);

            return new Transition<TState, TEvent>(GetGoto(statesMap), action, range);
        }

        private protected override bool HasSubTransitions() => slaves.Count > 0;

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent}.GotoCore(TState)"/>
        public StateBuilder<TState, TEvent> Goto(TState state)
        {
            GotoCore(state);
            return parent;
        }

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent}.GotoSelfCore(TState)"/>
        public StateBuilder<TState, TEvent> GotoSelf(TState state)
        {
            GotoSelf(state);
            return parent;
        }

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent}.StaySelfCore(TState)"/>
        public StateBuilder<TState, TEvent> StaySelf()
        {
            StaySelfCore();
            return parent;
        }

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent}.ExecuteCore(Action{object})"/>
        public MasterTransitionBuilder<TState, TEvent> Execute(Action<object> action)
        {
            ExecuteCore(action);
            return this;
        }

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent}.ExecuteCore(Action)"/>
        public MasterTransitionBuilder<TState, TEvent> Execute(Action action)
        {
            ExecuteCore(action);
            return this;
        }
    }
}