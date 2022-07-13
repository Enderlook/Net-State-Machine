namespace Enderlook.StateMachine;

/// <summary>
/// Determines the excution order of subscribed delegates of <see cref="StateBuilder{TState, TEvent, TRecipient}.OnEntry(System.Action)"/> (and overloads) during the initialization of the state machine.<br/>
/// This determines the order of execution of delegates from the initial state set by <see cref="StateMachineBuilder{TState, TEvent, TRecipient}.SetInitialState(TState, InitializationPolicy)"/> (and parent states if the initial state is a substate).
/// </summary>
public enum InitializationPolicy
{
    /// <summary>
    /// Determines that subscribed delegates should not run.
    /// </summary>
    Ignore = 0,

    /// <summary>
    /// Determines that subscribed delegates on parents are run first.<br/>
    /// For example:
    /// <c>... -> ParentOf(ParentOf(n)) -> ParentOf(n) -> n</c>.
    /// </summary>
    ParentFirst = 1,

    /// <summary>
    /// Determines that subscribed delegates on children are run first.<br/>
    /// For example:
    /// <c>n -> ParentOf(n) -> ParentOf(ParentOf(n)) -> ...</c>.
    /// </summary>
    ChildFirst = 2,

    /// <summary>
    /// Determines that only the subscribed delegates of the current state are run.<br/>
    /// Delegates subscribed on parent states won't be run.
    /// For example:
    /// <c>n</c>,
    /// </summary>
    Current = 3,
}
