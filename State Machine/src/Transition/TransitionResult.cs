namespace Enderlook.StateMachine;

internal enum TransitionResult : byte
{
    Continue,
    Branch,
    GoTo,
    StaySelf
}
