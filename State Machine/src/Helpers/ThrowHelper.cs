using System;
using System.Diagnostics.CodeAnalysis;

namespace Enderlook.StateMachine;

internal static class ThrowHelper
{
    public static void ThrowArgumentException_AlreadyHasEvent()
        => throw new ArgumentException("The event was already registered for this state.");

    public static void ThrowArgumentException_AlreadyHasInitialSubState()
        => throw new ArgumentException("The intial sub state was already registered.", "state");

    public static void ThrowArgumentException_AlreadyHasState()
        => throw new ArgumentException("The state was already registered.", "state");

    public static void ThrowArgumentException_AlreadyIsSubState()
        => throw new ArgumentException("The sub state was already registered.", "state");

    public static void ThrowArgumentException_StateNotFound()
        => throw new ArgumentException("The specified state was not found in the state machine.", "state");

    [DoesNotReturn]
    public static void ThrowArgumentNullException_Action()
        => throw new ArgumentNullException("action");

    [DoesNotReturn]
    public static void ThrowArgumentNullException_Event()
        => throw new ArgumentNullException("event");

    [DoesNotReturn]
    public static void ThrowArgumentNullException_Parameters()
        => throw new ArgumentNullException("parameters");

    [DoesNotReturn]
    public static void ThrowArgumentNullException_State()
        => throw new ArgumentNullException("state");

    [DoesNotReturn]
    public static void ThrowArgumentNullException_Guard()
        => throw new ArgumentNullException("guard");

    public static void ThrowArgumentOutOfRangeException_Index()
        => throw new ArgumentOutOfRangeException("index");

    public static void ThrowInvalidOperationException_AlreadyHasFinalized()
        => throw new InvalidOperationException("The configuration of this state machine builder has already been finalized.");

    public static void ThrowInvalidOperationException_AlreadyHasInitialState()
        => throw new InvalidOperationException("Already has registered initial state.");

    public static void ThrowInvalidOperationException_DoesNotHaveInitialState()
        => throw new InvalidOperationException("The state machine builder doesn't have registered an initial state.");

    public static void ThrowInvalidOperationException_DoesNotHaveRegisteredStates()
        => throw new InvalidOperationException("The state machine builder doesn't have registered any state.");

    public static void ThrowInvalidOperationException_EventNotRegisterForState<TState, TEvent>(State<TState> state, TEvent @event)
        where TState : notnull
        where TEvent : notnull
        => throw new InvalidOperationException($"Not found a transition for event {@event} in current state {state}.");

    public static void ThrowInvalidOperationException_TransitionGoesToANotRegisteredState()
        => throw new InvalidOperationException("A transition builder from the state machine builder has a transition to an state that was not registered in the state machine builder.");

    public static void ThrowInvalidOperationException_TransitionMustHaveTerminator()
        => throw new InvalidOperationException("A transition builder from the state machine builder doesn't have a terminator, i.e: doesn't end with a call to GoTo(TState) nor StaySelf().");

    public static void ThrowInvalidOperationException_StateIsSubStateOfANotRegisteredState()
        => throw new InvalidOperationException("An state builder is a substate of an state which is not registered in the state machine builder.");
}
