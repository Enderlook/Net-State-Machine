using System;

namespace Enderlook.StateMachine;

[Flags]
internal enum TransitionEventType : byte
{
    Empty = 0,
    HasRecipient = 1 << 0,
    HasParameter = 1 << 1,
    IsBranch = 1 << 2,
    IsGoTo = 1 << 3,
    IsStaySelf = 1 << 4,
}
