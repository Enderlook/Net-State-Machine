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
    /// <typeparam name="TParent">Type of parent which creates this instance.</typeparam>
    public class SlaveTransitionBuilder<TState, TEvent, TParent> : TransitionBuilder<TState, TEvent>
        where TState : IComparable
        where TEvent : IComparable
        where TParent : TransitionBuilder<TState, TEvent>
    {
        private Delegate guard;
        private TParent parent;
        private List<SlaveTransitionBuilder<TState, TEvent, SlaveTransitionBuilder<TState, TEvent, TParent>>> slaves;
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
        public SlaveTransitionBuilder<TState, TEvent, SlaveTransitionBuilder<TState, TEvent, TParent>> If(Func<bool> guard)
            => If((Delegate)guard);

        /// <inheritdoc cref="If(Delegate)"/>
        public SlaveTransitionBuilder<TState, TEvent, SlaveTransitionBuilder<TState, TEvent, TParent>> If(Func<object, bool> guard)
            => If((Delegate)guard);

        /// <summary>
        /// Add a sub transition with a condition.
        /// </summary>
        /// <param name="guard">Condition to execute transition.</param>
        /// <returns>Sub transition.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="guard"/> is <see langword="null"/>.</exception>
        private SlaveTransitionBuilder<TState, TEvent, SlaveTransitionBuilder<TState, TEvent, TParent>> If(Delegate guard)
        {
            SlaveTransitionBuilder<TState, TEvent, SlaveTransitionBuilder<TState, TEvent, TParent>> slave = new SlaveTransitionBuilder<TState, TEvent, SlaveTransitionBuilder<TState, TEvent, TParent>>(guard, this);
            if (slaves is null)
                slaves = new List<SlaveTransitionBuilder<TState, TEvent, SlaveTransitionBuilder<TState, TEvent, TParent>>>();
            slaves.Add(slave);
            return slave;
        }

        internal override Transition<TState, TEvent> ToTransition(ListSlot<Transition<TState, TEvent>> transitions, Dictionary<TState, int> statesMap)
        {
            if (slaves == null)
                return new Transition<TState, TEvent>(GetGoto(statesMap), action, (0, 0), guard);
            (int from, int to) range = transitions.Reserve(slaves.Count);

            int i = range.from;
            foreach (SlaveTransitionBuilder<TState, TEvent, SlaveTransitionBuilder<TState, TEvent, TParent>> slave in slaves)
            {
                transitions.Store(slave.ToTransition(transitions, statesMap), i);
                i++;
            }
            Debug.Assert(i == range.to);

            return new Transition<TState, TEvent>(GetGoto(statesMap), action, range, guard);
        }

        private protected override bool HasSubTransitions() => slaves.Count > 0;

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent}.GotoCore(TState)"/>
        public TParent Goto(TState state)
        {
            GotoCore(state);
            return parent;
        }

        public TParent GotoSelf(TState state)
        /// <inheritdoc cref="TransitionBuilder{TState, TEvent}.GotoSelfCore()"/>
        {
            GotoSelf(state);
            return parent;
        }

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent}.StaySelfCore()"/>
        public TParent StaySelf()
        {
            StaySelfCore();
            return parent;
        }

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent}.ExecuteCore(Delegate)"/>
        public SlaveTransitionBuilder<TState, TEvent, TParent> Execute(Action<object> action)
        {
            ExecuteCore(action);
            return this;
        }

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent}.ExecuteCore(Delegate)"/>
        public SlaveTransitionBuilder<TState, TEvent, TParent> Execute(Action action)
        {
            ExecuteCore(action);
            return this;
        }
    }
}