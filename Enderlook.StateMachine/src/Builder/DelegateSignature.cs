using System;

namespace Enderlook.StateMachine;

[Flags]
internal enum DelegateSignature : byte
{
    Empty = default,
    HasRecipient = 1 << 0,
    HasParameter = 1 << 1,
}
