using System;
using System.Diagnostics.CodeAnalysis;

namespace Enderlook.StateMachine;

internal static class ThrowHelper
{
    public static void ThrowArgumentException_AlreadyHasEvent()
        => throw new ArgumentException("The event was already registered for this state.");

    public static void ThrowArgumentException_StateCanNotBeSubStateOfItself()
        => throw new ArgumentException("Can't be substate of itself", "state");

    public static void ThrowArgumentException_StateNotFound()
        => throw new ArgumentException("The specified state was not found in the state machine.", "state");

    [DoesNotReturn]
    public static void ThrowArgumentNullException_Action()
        => throw new ArgumentNullException("action");

    [DoesNotReturn]
    public static void ThrowArgumentNullException_Event()
        => throw new ArgumentNullException("event");

    [DoesNotReturn]
    public static void ThrowArgumentNullException_Factory()
        => throw new ArgumentNullException("factory");

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

    public static void ThrowInvalidOperationException_AParameterBuilderHasNotBeenFinalized()
        => throw new InvalidOperationException("A parameters builder associated with this state machine has not been finalized.");

    public static void ThrowInvalidOperationException_AlreadyHasFinalized()
        => throw new InvalidOperationException("The configuration of this state machine builder has already been finalized.");

    public static void ThrowInvalidOperationException_AlreadyHasGoto()
        => throw new InvalidOperationException("Already has defined a goto state.");

    public static void ThrowInvalidOperationException_AlreadyHasInitialState()
        => throw new InvalidOperationException("Already has registered initial state.");

    public static void ThrowInvalidOperationException_AlreadyHasPolicy()
        => throw new InvalidOperationException("The policy of this transition in respect of the specified subscribed delegates was already been configured");

    public static void ThrowInvalidOperationException_AlreadyIsSubState()
        => throw new InvalidOperationException("The sub state was already registered.");

    public static void ThrowInvalidOperationException_CircularReferenceOfSubstates()
        => throw new InvalidOperationException("The state machine builder has an state which is a substate that performs a circular reference.");

    public static void ThrowInvalidOperationException_DoesNotHaveInitialState()
        => throw new InvalidOperationException("The state machine builder doesn't have registered an initial state.");

    public static void ThrowInvalidOperationException_DoesNotHaveRegisteredStateInGoto()
        => throw new InvalidOperationException("The state machine builder has a goto transition which doesn't have registered any state to go.");

    public static void ThrowInvalidOperationException_DoesNotHaveRegisteredStates()
        => throw new InvalidOperationException("The state machine builder doesn't have registered any state.");

    public static void ThrowInvalidOperationException_EventNotRegisterForState<TState, TEvent>(TState state, TEvent @event)
        where TState : notnull
        where TEvent : notnull
        => throw new InvalidOperationException($"Not found a transition for event {@event} in current state {state}.");

    public static void ThrowInvalidOperationException_StateWithStateARecipientAlreadyExist()
        => throw new InvalidOperationException("The specified state already exists with an state recipient.");

    public static void ThrowInvalidOperationException_ParameterBuilderWasFinalized()
        => throw new InvalidOperationException("The associated parameters builder has already been finalized.");

    public static void ThrowInvalidOperationException_TransitionGoesToANotRegisteredState()
        => throw new InvalidOperationException("A transition builder from the state machine builder has a transition to an state that was not registered in the state machine builder.");

    public static void ThrowInvalidOperationException_TransitionMustHaveTerminator()
        => throw new InvalidOperationException("A transition builder from the state machine builder doesn't have a terminator, i.e: doesn't end with a call to GoTo(TState) nor StaySelf().");

    [DoesNotReturn]
    public static void ThrowInvalidOperationException_StateIsSubStateOfANotRegisteredState()
        => throw new InvalidOperationException("An state builder is a substate of an state which is not registered in the state machine builder.");
}
