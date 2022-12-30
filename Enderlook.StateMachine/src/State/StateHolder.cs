using System;

namespace Enderlook.StateMachine;

internal abstract class StateHolder
{
    public abstract void Invoke(Delegate @delegate);
}
