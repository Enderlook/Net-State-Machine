namespace Enderlook.StateMachine;

internal interface IStateMachineBuilderReacher<TState, TEvent, TRecipient>
    where TState : notnull
    where TEvent : notnull
{
    StateMachineBuilder<TState, TEvent, TRecipient> StateMachineBuilder { get; }
}
