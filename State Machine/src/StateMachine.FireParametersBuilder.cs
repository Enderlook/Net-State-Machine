using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

public sealed partial class StateMachine<TState, TEvent, TRecipient>
    where TState : notnull
    where TEvent : notnull
{
    /// <summary>
    /// Parameters builder of method <see cref="FireWithParameters(TEvent)"/>.
    /// </summary>
    public readonly struct FireParametersBuilder
    {
        private readonly StateMachine<TState, TEvent, TRecipient> stateMachine;
        private readonly TEvent @event;
        private readonly int parameterBuilderVersion;

        internal FireParametersBuilder(StateMachine<TState, TEvent, TRecipient> stateMachine, TEvent @event)
        {
            this.stateMachine = stateMachine;
            this.@event = @event;
            parameterBuilderVersion = ++stateMachine.parameterBuilderVersion;
        }

        /// <summary>
        /// Stores a parameter that can be passed to callbacks.
        /// </summary>
        /// <typeparam name="TParameter">Type of parameter.</typeparam>
        /// <param name="parameter">Parameter than can be passed to callbacks.</param>
        /// <returns><see langword="this"/>.</returns>
        public FireParametersBuilder With<TParameter>(TParameter parameter)
        {
            if (stateMachine.parameterBuilderVersion != parameterBuilderVersion) ThrowHelper.ThrowInvalidOperationException_ParameterBuilderWasFinalized();
            if (!stateMachine.parameters.TryGetValue(typeof(TParameter), out ParameterSlots? container))
                stateMachine.parameters.Add(typeof(TParameter), container = new ParameterSlots<TParameter>());
            Debug.Assert(container is ParameterSlots<TParameter>);
            int index = Unsafe.As<ParameterSlots<TParameter>>(container).Store(parameter, false);
            ParameterSlot slot = new(container, index);
            stateMachine.parameterBuilderFirstIndex = stateMachine.parameterIndexes.StoreLast(slot, stateMachine.parameterBuilderFirstIndex != -1);
            return this;
        }

        /// <summary>
        /// Fires the event.
        /// </summary>
        public void Done()
        {
            if (stateMachine.parameterBuilderVersion != parameterBuilderVersion) ThrowHelper.ThrowInvalidOperationException_ParameterBuilderWasFinalized();
            int index = stateMachine.parameterBuilderFirstIndex;
            stateMachine.parameterBuilderFirstIndex = -1;
            stateMachine.EnqueueAndRunIfNotRunning(@event, index);
        }
    }
}