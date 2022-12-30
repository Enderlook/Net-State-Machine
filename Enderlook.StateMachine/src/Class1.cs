using System;
using System.Collections.Generic;
using System.Text;

namespace Enderlook.StateMachine;

public interface IAction
{
    void Invoke<TParameter>(TParameter parameter);
}

public interface IAction<TRecipient>
{
    void Invoke<TParameter>(TRecipient recipient, TParameter parameter);
}
