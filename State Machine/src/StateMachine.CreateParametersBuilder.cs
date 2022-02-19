using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

public sealed partial class StateMachine<TState, TEvent, TRecipient>
    where TState : notnull
    where TEvent : notnull
{
    /// <summary>
    /// Parameters builder of method <see cref="StateMachineFactory{TState, TEvent, TRecipient}.CreateWithParameters(TRecipient)"/>.
    /// </summary>
    public readonly struct CreateParametersBuilder
    {
        private readonly StateMachine<TState, TEvent, TRecipient> stateMachine;

        internal CreateParametersBuilder(StateMachine<TState, TEvent, TRecipient> stateMachine) => this.stateMachine = stateMachine;

        /// <summary>
        /// Stores a parameter that can be passed to callbacks.
        /// </summary>
        /// <typeparam name="TParameter">Type of parameter.</typeparam>
        /// <param name="parameter">Parameter than can be passed to callbacks.</param>
        /// <returns><see langword="this"/>.</returns>
        public CreateParametersBuilder With<TParameter>(TParameter parameter)
        {
            if (!stateMachine.parameters.TryGetValue(typeof(TParameter), out ParameterSlots? container))
                stateMachine.parameters.Add(typeof(TParameter), container = new ParameterSlots<TParameter>());
            Debug.Assert(container is ParameterSlots<TParameter>);
            int index = Unsafe.As<ParameterSlots<TParameter>>(container).Store(parameter, false);
            ParameterSlot slot = new(container, index);
            if (stateMachine.parameterBuilderFirstIndex != -1)
                stateMachine.parameterIndexes.StoreLast(slot, true);
            else
                stateMachine.parameterBuilderFirstIndex = stateMachine.parameterIndexes.StoreLast(slot, false);
            return this;
        }

        /// <summary>
        /// Initialized the state machine.
        /// </summary>
        public StateMachine<TState, TEvent, TRecipient> Done()
        {
            int index = stateMachine.parameterBuilderFirstIndex;
            if (index != -1)
            {
                stateMachine.parameterBuilderFirstIndex = -1;
                if (stateMachine.flyweight.RunEntryActionsOfInitialState)
                    stateMachine.RunEntryAndDisposeParameters(stateMachine.currentState, stateMachine.parameterIndexes.GetEnumeratorStartingAt(index));
                else
                    stateMachine.RemoveParameters(index);
                return stateMachine;
            }
            else
            {
                if (stateMachine.flyweight.RunEntryActionsOfInitialState)
                    stateMachine.RunEntryAndDisposeParameters(stateMachine.currentState, default);
                return stateMachine;
            }
        }
    }
}