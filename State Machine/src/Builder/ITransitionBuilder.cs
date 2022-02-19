using System.Collections.Generic;

namespace Enderlook.StateMachine;

internal interface ITransitionBuilder<TState>
{
    int GetTotalTransitionsAndEnsureHasTerminator();

    void Save(Dictionary<TState, int> statesMap, int currentState, TransitionEventUnion[] transitionEvents, ref int iTransitionEvents);
}
