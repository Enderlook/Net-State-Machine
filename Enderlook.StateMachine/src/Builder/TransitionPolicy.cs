namespace Enderlook.StateMachine;

/// <summary>
/// Determines the transition policy between two states.<br/>
/// This configures how subscribed delegates on states are run during transition between states.
/// </summary>
public enum TransitionPolicy
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
    /// Determines that subscribed delegates on parents are run first from (exluding) the last common parent between the two states.<br/>
    /// For example:<br/>
    /// If <c>ParentOf(ParentOf(n)) == ParentOf(ParentOf(m))</c>:<br/>
    /// <c>ParentOf(n) -> n</c>.<br/>
    /// If <c>ParentOf(ParentOf(n)) != ParentOf(ParentOf(m))</c>:<br/>
    /// <c>... -> ParentOf(ParentOf(n)) -> ParentOf(n) -> n</c>.<br/>
    /// If <c>n == m</c>:<br/>
    /// <c>... -> ParentOf(ParentOf(n)) -> ParentOf(n)</c>.
    /// </summary>
    ParentFirstWithCulling = 3,

    /// <summary>
    /// Determines that subscribed delegates on children are run first until reach (excluding) the last common parent between the two states.<br/>
    /// For example:<br/>
    /// If <c>ParentOf(ParentOf(n)) == ParentOf(ParentOf(m))</c>:<br/>
    /// <c>n -> ParentOf(n)</c>.<br/>
    /// If <c>ParentOf(ParentOf(n)) != ParentOf(ParentOf(m))</c>:<br/>
    /// <c>n -> ParentOf(n) -> ParentOf(ParentOf(n)) -> ...</c>.<br/>
    /// If <c>n == m</c>:<br/>
    /// <c>ParentOf(n) --> ParentOf(ParentOf(n)) -> ...</c>.
    /// </summary>
    ChildFirstWithCulling = 4,

    /// <summary>
    /// Determines that subscribed delegates on parents are run first from (including) the last common parent between the two states.<br/>
    /// For example:<br/>
    /// If <c>ParentOf(ParentOf(n)) == ParentOf(ParentOf(m))</c>:<br/>
    /// <c>ParentOf(ParentOf(n)) -> ParentOf(n) -> n</c>.<br/>
    /// If <c>ParentOf(ParentOf(n)) != ParentOf(ParentOf(m))</c>:<br/>
    /// <c>... -> ParentOf(ParentOf(n)) -> ParentOf(n) -> n</c>.<br/>
    /// If <c>n == m</c>:<br/>
    /// <c>ParentOf(ParentOf(n)) -> ParentOf(n) -> n</c>.
    /// </summary>
    ParentFirstWithCullingInclusive = 5,

    /// <summary>
    /// Determines that subscribed delegates on children are run first until reach (including) the last common parent between the two states.<br/>
    /// For example:<br/>
    /// If <c>ParentOf(ParentOf(n)) == ParentOf(ParentOf(m))</c>:<br/>
    /// <c>n -> ParentOf(n)</c> -> ParentOf(ParentOf(n)).<br/>
    /// If <c>ParentOf(ParentOf(n)) != ParentOf(ParentOf(m))</c>:<br/>
    /// <c>n -> ParentOf(n) -> ParentOf(ParentOf(n)) -> ...</c>.<br/>
    /// If <c>n == m</c>:<br/>
    /// <c>n -> ParentOf(n) --> ParentOf(ParentOf(n)) -> ...</c>.
    /// </summary>
    ChildFirstWithCullingInclusive = 6,
}