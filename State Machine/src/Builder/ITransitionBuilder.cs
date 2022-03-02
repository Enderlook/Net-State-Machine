using System.Collections.Generic;

namespace Enderlook.StateMachine;

internal interface ITransitionBuilder<TState, TEvent, TRecipient>
    where TState : notnull
    where TEvent : notnull
{
    int GetTotalTransitionsAndEnsureHasTerminator(Dictionary<TState, StateBuilder<TState, TEvent, TRecipient>> states);

    void Save(Dictionary<TState, StateBuilder<TState, TEvent, TRecipient>> states, Dictionary<TState, int> statesMap, int currentStateIndex, StateBuilder<TState, TEvent, TRecipient> currentStateBuilder, TransitionEventUnion[] transitionEvents, ref int iTransitionEvents);

    StateBuilder<TState, TEvent, TRecipient> StateBuilder { get; }
}
