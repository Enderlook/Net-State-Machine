using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

internal readonly struct State<TState>
{
    public readonly TState state;
    private readonly int subStateOf; // -1 if is not a substate.
    private readonly int onEventsStart;
    public readonly int onUpdateLength;
    public readonly int onEntryLength;
    public readonly int onExitLength;

    public int OnUpdateStart
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => onEventsStart;
    }

    public int OnEntryStart
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => onEventsStart + onUpdateLength;
    }

    public int OnExitStart
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => onEventsStart + onUpdateLength + onEntryLength;
    }

    public State(TState state, int subStateOf, int onEventsStart, int onUpdateLength, int onEntryLength, int onExitLength)
    {
        this.state = state;
        this.subStateOf = subStateOf;
        this.onEventsStart = onEventsStart;
        this.onUpdateLength = onUpdateLength;
        this.onEntryLength = onEntryLength;
        this.onExitLength = onExitLength;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetParentState(out int parent)
    {
        parent = subStateOf;
        return subStateOf != -1;
    }
}
