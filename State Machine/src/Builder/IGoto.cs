using System.Diagnostics.CodeAnalysis;

namespace Enderlook.StateMachine;

internal interface IGoto<TState>
{
    bool TryGetState([NotNullWhen(true)] out TState? state);

    TransitionPolicy OnEntryPolicy { get; }

    TransitionPolicy OnExitPolicy { get; }

    void Validate();
}