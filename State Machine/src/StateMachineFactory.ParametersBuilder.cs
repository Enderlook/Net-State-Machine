namespace Enderlook.StateMachine;

public sealed partial class StateMachineFactory<TState, TEvent, TRecipient>
    where TState : notnull
    where TEvent : notnull
{
    /// <summary>
    /// Creates a configured and initialized <see cref="StateMachine{TState, TEvent, TRecipient}"/> using the configuration provided by this factory.<br/>
    /// This method is thread-safe.<br/>
    /// Additionally, this methods allows to store a parameter that will be passed to the subscribed delegates of the on entry of the initial state (this is ignored is the factory was configured to do so).
    /// </summary>
    /// <typeparam name="TParameter">Parameter type.</typeparam>
    /// <param name="parameter">Parameter to store.</param>
    /// <returns>A builder of parameters to store that will be passed during the creation of the instance.</returns>
    public ParametersBuilder With<TParameter>(TParameter parameter)
    {
        StateMachine<TState, TEvent, TRecipient> stateMachine = new(this);
        stateMachine.StoreFirstParameter(parameter);
        return new(stateMachine);
    }

    /// <summary>
    /// Parameters builder of method <see cref="StateMachineFactory{TState, TEvent, TRecipient}.With{TParameter}(TParameter)"/>.
    /// </summary>
    public readonly struct ParametersBuilder
    {
        private readonly StateMachine<TState, TEvent, TRecipient> stateMachine;

        internal ParametersBuilder(StateMachine<TState, TEvent, TRecipient> stateMachine) => this.stateMachine = stateMachine;

        /// <summary>
        /// Stores a parameter that can be passed to callbacks.
        /// </summary>
        /// <typeparam name="TParameter">Type of parameter.</typeparam>
        /// <param name="parameter">Parameter than can be passed to callbacks.</param>
        /// <returns><see langword="this"/>.</returns>
        public ParametersBuilder With<TParameter>(TParameter parameter)
        {
            stateMachine.StoreParameter(parameter);
            return this;
        }

        /// <inheritdoc cref="StateMachineFactory{TState, TEvent, TRecipient}.Create(TRecipient)"/>
        public StateMachine<TState, TEvent, TRecipient> Create(TRecipient recipient)
        {
            stateMachine.Initialize(recipient);
            return stateMachine;
        }
    }
}