using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Enderlook.StateMachine
{
    /// <summary>
    /// Builder of concrete slave transitions.
    /// </summary>
    /// <typeparam name="TState">Type that determines states.</typeparam>
    /// <typeparam name="TEvent">Type that determines events.</typeparam>
    /// <typeparam name="TParameter">Type that determines common ground for parameters.</typeparam>
    /// <typeparam name="TParent">Type of parent which creates this instance.</typeparam>
    public class SlaveTransitionBuilder<TState, TEvent, TParameter, TParent> : TransitionBuilder<TState, TEvent>
        where TState : IComparable
        where TEvent : IComparable
        where TParent : TransitionBuilder<TState, TEvent>
    {
        private Delegate guard;
        private TParent parent;
        private List<SlaveTransitionBuilder<TState, TEvent, TParameter, SlaveTransitionBuilder<TState, TEvent, TParameter, TParent>>> slaves;
        internal override TState SelfState => parent.SelfState;

        internal SlaveTransitionBuilder(Delegate guard, TParent parent)
        {
            if (guard is null)
                throw new ArgumentNullException(nameof(guard));
            this.guard = guard;
            this.parent = parent;
        }

        /// <summary>
        /// Escapes up to the transition which created this instance.
        /// </summary>
        /// <returns>Builders of this instance.</returns>
        public TParent Otherwise() => parent;

        /// <inheritdoc cref="If(Delegate)"/>
        public SlaveTransitionBuilder<TState, TEvent, TParameter, SlaveTransitionBuilder<TState, TEvent, TParameter, TParent>> If(Func<bool> guard)
            => If((Delegate)guard);

        /// <inheritdoc cref="If(Delegate)"/>
        public SlaveTransitionBuilder<TState, TEvent, TParameter, SlaveTransitionBuilder<TState, TEvent, TParameter, TParent>> If(Func<TParameter, bool> guard)
            => If((Delegate)guard);

        /// <summary>
        /// Add a sub transition with a condition.
        /// </summary>
        /// <param name="guard">Condition to execute transition.</param>
        /// <returns>Sub transition.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="guard"/> is <see langword="null"/>.</exception>
        private SlaveTransitionBuilder<TState, TEvent, TParameter, SlaveTransitionBuilder<TState, TEvent, TParameter, TParent>> If(Delegate guard)
        {
            SlaveTransitionBuilder<TState, TEvent, TParameter, SlaveTransitionBuilder<TState, TEvent, TParameter, TParent>> slave = new SlaveTransitionBuilder<TState, TEvent, TParameter, SlaveTransitionBuilder<TState, TEvent, TParameter, TParent>>(guard, this);
            if (slaves is null)
                slaves = new List<SlaveTransitionBuilder<TState, TEvent, TParameter, SlaveTransitionBuilder<TState, TEvent, TParameter, TParent>>>();
            slaves.Add(slave);
            return slave;
        }

        internal override Transition<TState, TEvent> ToTransition(ListSlot<Transition<TState, TEvent>> transitions, Dictionary<TState, int> statesMap, TState currentState)
        {
            if (slaves == null)
                return new Transition<TState, TEvent>(GetGoto(statesMap, currentState), action, (0, 0), guard);
            (int from, int to) range = transitions.Reserve(slaves.Count);

            int i = range.from;
            foreach (SlaveTransitionBuilder<TState, TEvent, TParameter, SlaveTransitionBuilder<TState, TEvent, TParameter, TParent>> slave in slaves)
            {
                transitions.Store(slave.ToTransition(transitions, statesMap, currentState), i);
                i++;
            }
            Debug.Assert(i == range.to);

            return new Transition<TState, TEvent>(GetGoto(statesMap, currentState), action, range, guard);
        }

        private protected override bool HasSubTransitions() => slaves.Count > 0;

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent}.GotoCore(TState)"/>
        public TParent Goto(TState state)
        {
            GotoCore(state);
            return parent;
        }

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent}.GotoSelfCore()"/>
        public TParent GotoSelf()
        {
            GotoSelfCore();
            return parent;
        }

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent}.StaySelfCore()"/>
        public TParent StaySelf()
        {
            StaySelfCore();
            return parent;
        }

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent}.ExecuteCore(Delegate)"/>
        public SlaveTransitionBuilder<TState, TEvent, TParameter, TParent> Execute(Action<TParameter> action)
        {
            ExecuteCore(action);
            return this;
        }

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent}.ExecuteCore(Delegate)"/>
        public SlaveTransitionBuilder<TState, TEvent, TParameter, TParent> Execute(Action action)
        {
            ExecuteCore(action);
            return this;
        }
    }
}