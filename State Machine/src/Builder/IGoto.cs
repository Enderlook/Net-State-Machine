namespace Enderlook.StateMachine;

internal interface IGoto<TState>
{
    TState? State { get; }

    TransitionPolicy OnEntryPolicy { get; }

    TransitionPolicy OnExitPolicy { get; }
}