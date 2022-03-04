namespace Enderlook.StateMachine;

/// <summary>
/// Determines the excution order of subscribed delegates.
/// </summary>
public enum ExecutionPolicy
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
