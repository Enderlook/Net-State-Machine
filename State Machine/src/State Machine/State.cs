using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine
{
    internal readonly struct State<TState, TEvent>
        where TState : IComparable
        where TEvent : IComparable
    {
        public readonly TState state;
        private readonly Delegate onEntry;
        private readonly Delegate onExit;
        private readonly Delegate onUpdate;
        private readonly Dictionary<TEvent, int> transitions;

        public State(TState state, Delegate onEntry, Delegate onExit, Delegate onUpdate, Dictionary<TEvent, int> transitions)
        {
            this.state = state;
            this.onEntry = onEntry;
            this.onExit = onExit;
            this.onUpdate = onUpdate;
            this.transitions = transitions;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunExit<TParameter>(TParameter parameter) => Helper.ExecuteVoid(onExit, parameter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunEntry<TParameter>(TParameter parameter) => Helper.ExecuteVoid(onEntry, parameter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunUpdate<TParameter>(TParameter parameter) => Helper.ExecuteVoid(onUpdate, parameter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetTransitionIndex(TEvent @event)
        {
            if (transitions.TryGetValue(@event, out int transitionIndex))
                return transitionIndex;
            throw new ArgumentException($"State {state} doesn't have any transition with event {@event}");
        }
    }
}