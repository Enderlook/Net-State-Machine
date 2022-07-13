using System.Collections.Generic;

namespace Enderlook.StateMachine;

internal static class EmptyDictionary<T>
{
    public static readonly Dictionary<T, T> Empty = new();
}
