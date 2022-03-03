using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.StateMachine;

public sealed partial class StateMachine<TState, TEvent, TRecipient>
    where TState : notnull
    where TEvent : notnull
{
    /// <summary>
    /// Stores a parameter(s) to pass on the next method call.
    /// </summary>
    /// <typeparam name="TParameter">Parameter type.</typeparam>
    /// <param name="parameter">Parameter to store.</param>
    /// <returns>A builder of parameters to store.</returns>
    public ParametersBuilder With<TParameter>(TParameter parameter)
    {
        if (parameterBuilderFirstIndex != -1) ThrowHelper.ThrowInvalidOperationException_AParameterBuilderHasNotBeenFinalized();
        int parameterBuilderVersion = ++this.parameterBuilderVersion;
        StoreFirstParameterInBuilder(parameter);
        return new(this, parameterBuilderVersion);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void StoreParameter<TParameter>(TParameter parameter)
    {
        if (!parameters.TryGetValue(typeof(TParameter), out ParameterSlots? container))
            container = CreateParameterSlot<TParameter>();
        Debug.Assert(container is ParameterSlots<TParameter>);
        int index = Unsafe.As<ParameterSlots<TParameter>>(container).Store(parameter, false);
        parameterIndexes.StoreLast(new(container, index), true);
    }

    /// <summary>
    /// Parameters builder of methods <see cref="StateMachine{TState, TEvent, TRecipient}.Fire(TEvent)"/>, <see cref="StateMachine{TState, TEvent, TRecipient}.FireImmediately(TEvent)"/> and <see cref="StateMachine{TState, TEvent, TRecipient}.Update()"/>.
    /// </summary>
    public readonly struct ParametersBuilder
    {
        private readonly StateMachine<TState, TEvent, TRecipient> stateMachine;
        private readonly int parameterBuilderVersion;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ParametersBuilder(StateMachine<TState, TEvent, TRecipient> stateMachine, int parameterBuilderVersion)
        {
            this.stateMachine = stateMachine;
            this.parameterBuilderVersion = parameterBuilderVersion;
        }

        /// <summary>
        /// Stores a parameter that can be passed to callbacks.
        /// </summary>
        /// <typeparam name="TParameter">Type of parameter.</typeparam>
        /// <param name="parameter">Parameter than can be passed to callbacks.</param>
        /// <returns><see langword="this"/>.</returns>
        public ParametersBuilder With<TParameter>(TParameter parameter)
        {
            if (stateMachine.parameterBuilderVersion != parameterBuilderVersion) ThrowHelper.ThrowInvalidOperationException_ParameterBuilderWasFinalized();
            stateMachine.StoreParameter(parameter);
            return this;
        }

        /// <inheritdoc cref="StateMachine{TState, TEvent, TRecipient}.Fire(TEvent)"/>
        public void Fire(TEvent @event)
        {
            if (stateMachine.parameterBuilderVersion != parameterBuilderVersion) ThrowHelper.ThrowInvalidOperationException_ParameterBuilderWasFinalized();
            int index = stateMachine.parameterBuilderFirstIndex;
            stateMachine.parameterBuilderFirstIndex = -1;
            stateMachine.EnqueueAndRunIfNotRunning(@event, index);
        }

        /// <inheritdoc cref="StateMachine{TState, TEvent, TRecipient}.FireImmediately(TEvent)"/>
        public void FireImmediately(TEvent @event)
        {
            if (stateMachine.parameterBuilderVersion != parameterBuilderVersion) ThrowHelper.ThrowInvalidOperationException_ParameterBuilderWasFinalized();
            int index = stateMachine.parameterBuilderFirstIndex;
            stateMachine.parameterBuilderFirstIndex = -1;
            stateMachine.EnqueueAndRun(@event, index);
        }

        /// <inheritdoc cref="StateMachine{TState, TEvent, TRecipient}.Update()"/>
        public void Update()
        {
            if (stateMachine.parameterBuilderVersion != parameterBuilderVersion) ThrowHelper.ThrowInvalidOperationException_ParameterBuilderWasFinalized();
            int index = stateMachine.parameterBuilderFirstIndex;
            stateMachine.parameterBuilderFirstIndex = -1;
            stateMachine.Update(index);
        }
    }
}