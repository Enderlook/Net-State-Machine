namespace Enderlook.StateMachine;

internal readonly struct InvariantObject
{
    public readonly object? Value;

    public InvariantObject(object? value)
    {
        Value = value;
    }
}