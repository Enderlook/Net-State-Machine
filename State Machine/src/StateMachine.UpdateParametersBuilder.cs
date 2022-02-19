using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

public sealed partial class StateMachine<TState, TEvent, TRecipient>
    where TState : notnull
    where TEvent : notnull
{
    /// <summary>
    /// Parameters builder of method <see cref="UpdateWithParameters"/>.
    /// </summary>
    public readonly struct UpdateParametersBuilder
    {
        private readonly StateMachine<TState, TEvent, TRecipient> stateMachine;
        private readonly int parameterBuilderVersion;

        internal UpdateParametersBuilder(StateMachine<TState, TEvent, TRecipient> stateMachine)
        {
            this.stateMachine = stateMachine;
            parameterBuilderVersion = ++stateMachine.parameterBuilderVersion;
        }

        /// <summary>
        /// Stores a parameter that can be passed to callbacks.
        /// </summary>
        /// <typeparam name="TParameter">Type of parameter.</typeparam>
        /// <param name="parameter">Parameter than can be passed to callbacks.</param>
        /// <returns><see langword="this"/>.</returns>
        public UpdateParametersBuilder With<TParameter>(TParameter parameter)
        {
            if (stateMachine.parameterBuilderVersion != parameterBuilderVersion) ThrowHelper.ThrowInvalidOperationException_ParameterBuilderWasFinalized();
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
        /// Executes the update.
        /// </summary>
        public void Done()
        {
            if (stateMachine.parameterBuilderVersion != parameterBuilderVersion) ThrowHelper.ThrowInvalidOperationException_ParameterBuilderWasFinalized();
            int index = stateMachine.parameterBuilderFirstIndex;
            if (index != -1)
            {
                stateMachine.parameterBuilderFirstIndex = -1;
                stateMachine.Update(index);
            }
            else
                stateMachine.RunUpdate(stateMachine.currentState, default);
        }
    }
}