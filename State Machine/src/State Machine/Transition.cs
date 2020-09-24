using System;
using System.Diagnostics;

namespace Enderlook.StateMachine
{
    internal struct Transition<TState, TEvent>
        where TState : IComparable
        where TEvent : IComparable
    {
        /// <summary>
        /// Determines to which state move.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Value</term>
        ///         <description>Description</description>
        ///     </listheader>
        ///     <item>
        ///         <term><c>>= 0</c></term>
        ///         <description>Move to that index.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>-1</c></term>
        ///         <description>Maintain in the same state.</description>
        ///     </item>
        /// </list>
        /// </summary>
        public int @goto;
        public Delegate action;
        public (int from, int to) transitions;
        public Delegate guard;
        public bool Maintain => @goto == -1;

        public Transition(int @goto, Delegate action, (int, int) transitions)
        {
            Debug.Assert(@goto >= -1);
            this.action = action;
            this.@goto = @goto;
            this.transitions = transitions;
            guard = null;
        }

        public Transition(int @goto, Delegate action, (int, int) transitions, Delegate guard)
            : this(@goto, action, transitions)
            => this.guard = guard;
    }
}