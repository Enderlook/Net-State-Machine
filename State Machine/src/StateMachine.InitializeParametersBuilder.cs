namespace Enderlook.StateMachine;
public partial class StateMachine<TState, TEvent, TRecipient>
{
    /// <summary>
    /// Parameters builder of method <see cref="StateMachineFactory{TState, TEvent, TRecipient}.With{TParameter}(TParameter)"/>.
    /// </summary>
    public readonly struct InitializeParametersBuilder
    {
        private readonly StateMachine<TState, TEvent, TRecipient> stateMachine;

        internal InitializeParametersBuilder(StateMachine<TState, TEvent, TRecipient> stateMachine) => this.stateMachine = stateMachine;

        /// <summary>
        /// Stores a parameter that can be passed to callbacks.
        /// </summary>
        /// <typeparam name="TParameter">Type of parameter.</typeparam>
        /// <param name="parameter">Parameter than can be passed to callbacks.</param>
        /// <returns><see langword="this"/>.</returns>
        public InitializeParametersBuilder With<TParameter>(TParameter parameter)
        {
            if (stateMachine.parameterBuilderVersion != 0) ThrowHelper.ThrowInvalidOperationException_ParameterBuilderWasFinalized();
            stateMachine.StoreParameter(parameter);
            return this;
        }

        /// <inheritdoc cref="StateMachineFactory{TState, TEvent, TRecipient}.Create(TRecipient)"/>
        public StateMachine<TState, TEvent, TRecipient> Create(TRecipient recipient)
        {
            if (stateMachine.parameterBuilderVersion != 0) ThrowHelper.ThrowInvalidOperationException_ParameterBuilderWasFinalized();
            stateMachine.Initialize(recipient);
            return stateMachine;
        }
    }
}