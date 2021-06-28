using System;
using System.Collections.Generic;

namespace Enderlook.StateMachine
{
    /// <summary>
    /// Builder of concrete transitions.
    /// </summary>
    /// <typeparam name="TState">Type that determines states.</typeparam>
    /// <typeparam name="TEvent">Type that determines events.</typeparam>
    /// <typeparam name="TParameter">Type that determines common ground for parameters.</typeparam>
    public abstract class TransitionBuilder<TState, TEvent, TParameter>
    {
        /// <summary>
        /// Determines the state of the <see cref="@goto"/>.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Value</term>
        ///         <description>Description</description>
        ///     </listheader>
        ///     <item>
        ///         <term><c>0</c></term>
        ///         <description>Uninitialized.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>1</c></term>
        ///         <description>Goto specified <see cref="@goto"/>.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>2</c></term>
        ///         <description>Maintain in the same state.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>3</c></term>
        ///         <description>Goto to the same state.</description>
        ///     </item>
        /// </list>
        /// </summary>
        protected int hasGoto;
        protected TState @goto;
        protected Action @do;
        protected Action<TParameter> doWithParameter;
        internal abstract TState SelfState { get; }

        /// <summary>
        /// Determines to which state this transition goes.
        /// </summary>
        /// <param name="state">State to move.</param>
        /// <returns>Creator of this instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when already has registered a goto.</exception>
        protected void GotoCore(TState state)
        {
            if (hasGoto != 0)
                throw new InvalidOperationException("Already has a registered goto.");

            @goto = state;
            hasGoto = 1;
        }

        /// <summary>
        /// Determines that this transition will go to the same state executing OnEntry and OnExit events.
        /// </summary>
        /// <returns>Creator of this instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when already has registered a goto.</exception>
        protected void GotoSelfCore()
        {
            if (hasGoto != 0)
                throw new InvalidOperationException("Already has a registered goto.");

            @goto = SelfState;
            hasGoto = 3;
        }

        /// <summary>
        /// Determines that this transition will not change state so no OnEntry nor OnExit event will be raised.
        /// </summary>
        /// <returns>Creator of this instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when already has registered a goto.</exception>
        protected void StaySelfCore()
        {
            if (hasGoto != 0)
                throw new InvalidOperationException("Already has a registered goto.");

            hasGoto = 2;
        }

        private protected abstract bool HasSubTransitions();

        /// <summary>
        /// Determines an action that is executed when the event is raised.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        /// <returns><see cref="this"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
        protected void DoCore(Action action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            @do += action;
        }

        /// <inheritdoc cref="DoCore(Action)"/>
        protected void DoCore(Action<TParameter> action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            doWithParameter += action;
        }

        /// <summary>
        /// Get the delegate to execute when on Do event.
        /// </summary>
        /// <returns>Delegate to execute.</returns>
        protected Delegate GetDo() => Helper.Combine(ref @do, ref doWithParameter);

        private protected int GetGoto(Dictionary<TState, int> statesMap, TState currentState)
        {
            if (hasGoto == 0)
                throw new InvalidOperationException("Transition must have registered a goto.");
            if (hasGoto == 2)
                return -1;
            if (hasGoto == 3)
                @goto = currentState;

            return @goto.TryGetStateIndex(statesMap);
        }

        internal abstract Transition<TState, TEvent> ToTransition(ListSlot<Transition<TState, TEvent>> transitions, Dictionary<TState, int> statesMap, TState currentState);
    }
}