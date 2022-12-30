using System;

namespace Enderlook.StateMachine;

[Flags]
internal enum StateEventType : byte
{
    Empty = 0,
    HasRecipient = 1 << 0,
    HasParameter = 1 << 1,
    HasStateRecipient = 1 << 2,
}

internal enum StateFactoryType : byte
{
    Null = 0,
    Empty = 1,
    HasRecipient = 2,
}

internal abstract class StateFactory<TRecipient>
{

}