using System;
using System.Collections.Generic;

namespace Enderlook.StateMachine
{
    internal readonly struct State<TState, TEvent>
        where TState : IComparable
        where TEvent : IComparable
    {
        public readonly TState state;
        public readonly Delegate onEntry;
        public readonly Delegate onExit;
        public readonly Delegate onUpdate;
        public readonly Dictionary<TEvent, int> transitions;

        public State(TState state, Delegate onEntry, Delegate onExit, Delegate onUpdate, Dictionary<TEvent, int> transitions)
        {
            this.state = state;
            this.onEntry = onEntry;
            this.onExit = onExit;
            this.onUpdate = onUpdate;
            this.transitions = transitions;
        }
    }
}