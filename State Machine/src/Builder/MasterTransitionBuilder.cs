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
    /// <typeparam name="TParameter">Type that determines common ground for parameters.</typeparam>
    public sealed class MasterTransitionBuilder<TState, TEvent, TParameter> : TransitionBuilder<TState, TEvent, TParameter>
    {
        private List<SlaveTransitionBuilder<TState, TEvent, TParameter, MasterTransitionBuilder<TState, TEvent, TParameter>>> slaves;
        private StateBuilder<TState, TEvent, TParameter> parent;
        internal override TState SelfState => parent.State;

        internal MasterTransitionBuilder(StateBuilder<TState, TEvent, TParameter> parent)
            => this.parent = parent;

        /// <inheritdoc cref="If(Delegate)"/>
        public SlaveTransitionBuilder<TState, TEvent, TParameter, MasterTransitionBuilder<TState, TEvent, TParameter>> If(Func<bool> guard)
            => If((Delegate)guard);

        /// <inheritdoc cref="If(Delegate)"/>
        public SlaveTransitionBuilder<TState, TEvent, TParameter, MasterTransitionBuilder<TState, TEvent, TParameter>> If(Func<TParameter, bool> guard)
            => If((Delegate)guard);

        /// <summary>
        /// Add a sub transition with a condition.
        /// </summary>
        /// <param name="guard">Condition to execute transition.</param>
        /// <returns>Sub transition.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="guard"/> is <see langword="null"/>.</exception>
        private SlaveTransitionBuilder<TState, TEvent, TParameter, MasterTransitionBuilder<TState, TEvent, TParameter>> If(Delegate guard)
        {
            SlaveTransitionBuilder<TState, TEvent, TParameter, MasterTransitionBuilder<TState, TEvent, TParameter>> slave = new SlaveTransitionBuilder<TState, TEvent, TParameter, MasterTransitionBuilder<TState, TEvent, TParameter>>(guard, this);
            if (slaves is null)
                slaves = new List<SlaveTransitionBuilder<TState, TEvent, TParameter, MasterTransitionBuilder<TState, TEvent, TParameter>>>();
            slaves.Add(slave);
            return slave;
        }

        internal override Transition<TState, TEvent> ToTransition(ListSlot<Transition<TState, TEvent>> transitions, Dictionary<TState, int> statesMap, TState currentState)
        {
            if (slaves == null)
                return new Transition<TState, TEvent>(GetGoto(statesMap, currentState), GetDo(), (0, 0));
            (int from, int to) range = transitions.Reserve(slaves.Count);

            int i = range.from;
            foreach (SlaveTransitionBuilder<TState, TEvent, TParameter, MasterTransitionBuilder<TState, TEvent, TParameter>> slave in slaves)
            {
                transitions.Store(slave.ToTransition(transitions, statesMap, currentState), i);
                i++;
            }
            Debug.Assert(i == range.to);

            return new Transition<TState, TEvent>(GetGoto(statesMap, currentState), GetDo(), range);
        }

        private protected override bool HasSubTransitions() => slaves.Count > 0;

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent, TParameter}.GotoCore(TState)"/>
        public StateBuilder<TState, TEvent, TParameter> Goto(TState state)
        {
            GotoCore(state);
            return parent;
        }

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent, TParameter}.GotoSelfCore()"/>
        public StateBuilder<TState, TEvent, TParameter> GotoSelf()
        {
            GotoSelfCore();
            return parent;
        }

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent, TParameter}.StaySelfCore()"/>
        public StateBuilder<TState, TEvent, TParameter> StaySelf()
        {
            StaySelfCore();
            return parent;
        }

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent, TParameter}.DoCore(Action{TParameter})"/>
        public MasterTransitionBuilder<TState, TEvent, TParameter> Do(Action<TParameter> action)
        {
            DoCore(action);
            return this;
        }

        /// <inheritdoc cref="TransitionBuilder{TState, TEvent, TParameter}.DoCore(Action)"/>
        public MasterTransitionBuilder<TState, TEvent, TParameter> Do(Action action)
        {
            DoCore(action);
            return this;
        }
    }
}