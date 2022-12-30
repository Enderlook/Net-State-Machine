using System;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

internal readonly struct State<TState>
{
    public readonly TState state;
    private readonly int subStateOf; // -1 if is not a substate.
    public readonly int onUpdateStart;
    public readonly int onUpdateLength;

    public State(TState state, int subStateOf, int onUpdateStart, int onUpdateLength)
    {
        this.state = state;
        this.subStateOf = subStateOf;
        this.onUpdateStart = onUpdateStart;
        this.onUpdateLength = onUpdateLength;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetParentState(out int parent)
    {
        parent = subStateOf;
        return subStateOf != -1;
    }
}
