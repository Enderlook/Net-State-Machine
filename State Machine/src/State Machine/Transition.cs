using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine
{
    internal readonly struct Transition<TState, TEvent>
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
        public readonly int @goto;
        private readonly Delegate action;
        public readonly (int from, int to) transitions;
        private readonly Delegate guard;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Run<TParameter>(TParameter parameter) => Helper.ExecuteVoid(action, parameter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGuard<TParameter>(TParameter parameter)
        {
            if (guard is null)
                return true;
            switch (guard)
            {
                case Func<bool> action:
                    return action();
                case Func<TParameter, bool> action:
                    return action(parameter);
            }
#if DEBUG
            Debug.Fail("Impossible State");
#endif
            return true;
        }

        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DebugEnsureNoGuard() => Debug.Assert(guard is null, "Master transition can't have guard.");
    }
}