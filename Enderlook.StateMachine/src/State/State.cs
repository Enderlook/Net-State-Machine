using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

internal readonly struct State<TState>
{
    public readonly TState state;
    private readonly int subStateOf; // -1 if is not a substate.
    public readonly int onUpdateStart;
    public readonly int onUpdateLength;
    public readonly int stateRecipientIndex;
    public readonly StateHelper? stateHelper;
    public readonly StateRecipientType stateRecipientType;
    private readonly Delegate? stateRecipientFactory;

    public State(TState state, int subStateOf, int onUpdateStart, int onUpdateLength, int stateRecipientIndex, StateHelper? stateHelper, StateRecipientType stateRecipientType, Delegate? stateRecipientFactory)
    {
        this.state = state;
        this.subStateOf = subStateOf;
        this.onUpdateStart = onUpdateStart;
        this.onUpdateLength = onUpdateLength;
        this.stateRecipientIndex = stateRecipientIndex;
        this.stateHelper = stateHelper;
        this.stateRecipientType = stateRecipientType;
        this.stateRecipientFactory = stateRecipientFactory;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetParentState(out int parent)
    {
        parent = subStateOf;
        return subStateOf != -1;
    }

    public object? CreateStateRecipient<TRecipient>(TRecipient recipient)
    {
        Debug.Assert(stateHelper is not null || (stateRecipientType == StateRecipientType.Unused && stateRecipientFactory is null));
        return stateHelper?.CreateStateRecipient(recipient, stateRecipientFactory);
    }
}