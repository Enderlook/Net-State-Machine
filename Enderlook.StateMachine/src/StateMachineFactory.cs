using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Enderlook.StateMachine;

/// <summary>
/// Factory of state machines.
/// </summary>
/// <typeparam name="TState">Type that determines states.</typeparam>
/// <typeparam name="TEvent">Type that determines events.</typeparam>
/// <typeparam name="TRecipient">Type that determines internal data that can be acceded by actions.</typeparam>
public sealed class StateMachineFactory<TState, TEvent, TRecipient>
    where TState : notnull
    where TEvent : notnull
{
    internal readonly State<TState>[] States;
    internal readonly StateEventUnion[] StateEvents;
    internal readonly TransitionEventUnion[] TransitionEvents;
    internal readonly Dictionary<(int State, TEvent Event), int> TransitionStartIndexes;
    internal readonly int InitialState;
    // -1 if doesn't have.
    internal readonly int InitialStateOnEntryStart;

    // This values are rarely used so we lazy initialize them.
    // Some of this values could be removed by using an additional indirection (using a Dictionary<TState, int>).
    // However since this values are shared between instances, it's not worth the memory saving.
    private Dictionary<TState, TState>? parentStates;
    private ReadOnlySlice<TState>[]? statesWithParents;
    private Dictionary<TState, ReadOnlySlice<TState>>? parentHierarchyStates;
    private ReadOnlySlice<TEvent>[]? eventsSupportedByStateIndex;
    private Dictionary<TState, ReadOnlySlice<TEvent>>? eventsSupportedByState;

    internal StateMachineFactory(State<TState>[] states, StateEventUnion[] stateEvents, TransitionEventUnion[] transitionEvents, Dictionary<(int State, TEvent Event), int> transitionStartIndexes, int initialState, int initialStateOnEntryStart)
    {
        States = states;
        StateEvents = stateEvents;
        TransitionEvents = transitionEvents;
        TransitionStartIndexes = transitionStartIndexes;
        InitialState = initialState;
        InitialStateOnEntryStart = initialStateOnEntryStart;
    }

    /// <summary>
    /// Creates a configured and initialized <see cref="StateMachine{TState, TEvent, TRecipient}"/> using the configuration provided by this factory.<br/>
    /// This method is thread-safe.
    /// </summary>
    /// <param name="recipient">Recipient for the new created <see cref="StateMachine{TState, TEvent, TRecipient}"/>.</param>
    /// <returns>New <see cref="StateMachine{TState, TEvent, TRecipient}"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StateMachine<TState, TEvent, TRecipient> Create(TRecipient recipient)
        => StateMachine<TState, TEvent, TRecipient>.From(this, recipient);

    /// <summary>
    /// Creates a configured and initialized <see cref="StateMachine{TState, TEvent, TRecipient}"/> using the configuration provided by this factory.<br/>
    /// This method is thread-safe.<br/>
    /// Additionally, this methods allows to store a parameter that will be passed to the subscribed delegates of the on entry of the initial state (this is ignored is the factory was configured to do so).
    /// </summary>
    /// <typeparam name="TParameter">Parameter type.</typeparam>
    /// <param name="parameter">Parameter to store.</param>
    /// <returns>A builder of parameters to store that will be passed during the creation of the instance.</returns>
    public StateMachine<TState, TEvent, TRecipient>.InitializeParametersBuilder With<TParameter>(TParameter parameter)
    {
        StateMachine<TState, TEvent, TRecipient> stateMachine = new(this);
        stateMachine.StoreFirstParameterInBuilder(parameter);
        return new(stateMachine);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ParentStateOf(TState state, [NotNullWhen(true)] out TState? parentState)
    {
        if (parentStates is Dictionary<TState, TState> parentStates_)
            return parentStates_.TryGetValue(state, out parentState);

        return Slow(out parentState);

        [MethodImpl(MethodImplOptions.NoInlining)]
        bool Slow([NotNullWhen(true)] out TState? parentState)
        {
            int subStates = 0;
            foreach (State<TState> element in States)
            {
                if (element.TryGetParentState(out _))
                    subStates++;
            }
            if (subStates == 0)
                parentStates = EmptyDictionary<TState>.Empty;
            else
            {
                Dictionary<TState, TState> parentStates_ = new();
                foreach (State<TState> element in States)
                {
                    if (element.TryGetParentState(out int parent))
                        parentStates_.Add(element.state, States[parent].state);
                }
                // We assing the field at the end to prevent racing conditions.
                parentStates = parentStates_;
            }

            return parentStates.TryGetValue(state, out parentState);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySlice<TState> GetHierarchyOfState(int state)
    {
        if (statesWithParents is ReadOnlySlice<TState>[] slices)
            return slices[state];

        return Slow();

        [MethodImpl(MethodImplOptions.NoInlining)]
        ReadOnlySlice<TState> Slow()
        {
            PopulateStatesWithParents();
            Debug.Assert(statesWithParents is not null);
            return statesWithParents[state];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySlice<TState> GetParentHierarchyOfState(TState state)
    {
        if (parentHierarchyStates is Dictionary<TState, ReadOnlySlice<TState>> parentHierarchyStates_)
        {
            if (parentHierarchyStates_.TryGetValue(state, out ReadOnlySlice<TState> parents))
                return parents;
            ThrowHelper.ThrowArgumentException_StateNotFound();
        }

        return Slow();

        [MethodImpl(MethodImplOptions.NoInlining)]
        ReadOnlySlice<TState> Slow()
        {
            if (statesWithParents is not ReadOnlySlice<TState>[] statesWithParents_)
            {
                PopulateStatesWithParents();
                Debug.Assert(statesWithParents is not null);
                statesWithParents_ = statesWithParents;
            }

            State<TState>[] states = States;
            int length = states.Length;
            Dictionary<TState, ReadOnlySlice<TState>> parentHierarchyStates_ = new(length);
            for (int i = 0; i < length; i++)
                parentHierarchyStates_.Add(states[i].state, statesWithParents_[i].WithoutFirst());
            // We assing the field at the end to prevent racing conditions.
            parentHierarchyStates = parentHierarchyStates_;
            if (parentHierarchyStates_.TryGetValue(state, out ReadOnlySlice<TState> parents))
                return parents;
            ThrowHelper.ThrowArgumentException_StateNotFound();
            Debug.Fail("Impossible state.");
            return default;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySlice<TEvent> GetAcceptedEventsByState(int state)
    {
        if (eventsSupportedByStateIndex is ReadOnlySlice<TEvent>[] eventsSupportedByStateIndex_)
            return eventsSupportedByStateIndex_[state];

        return Slow();

        [MethodImpl(MethodImplOptions.NoInlining)]
        ReadOnlySlice<TEvent> Slow()
        {
            PopulateEventsSupportedByStateIndex();
            Debug.Assert(eventsSupportedByStateIndex is not null);
            return eventsSupportedByStateIndex[state];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySlice<TEvent> GetAcceptedEventsByState(TState state)
    {
        if (eventsSupportedByState is Dictionary<TState, ReadOnlySlice<TEvent>> eventsSupportedByState_)
            return eventsSupportedByState_[state];

        return Slow();

        [MethodImpl(MethodImplOptions.NoInlining)]
        ReadOnlySlice<TEvent> Slow()
        {
            if (eventsSupportedByStateIndex is not ReadOnlySlice<TEvent>[] eventsSupportedByStateIndex_)
            {
                PopulateEventsSupportedByStateIndex();
                Debug.Assert(eventsSupportedByStateIndex is not null);
                eventsSupportedByStateIndex_ = eventsSupportedByStateIndex;
            }

            State<TState>[] states = States;
            int length = states.Length;
            Dictionary<TState, ReadOnlySlice<TEvent>> eventsSupportedByState_ = new(length);
            for (int i = 0; i < length; i++)
                eventsSupportedByState_.Add(states[i].state, eventsSupportedByStateIndex_[i]);

            // We assing the field at the end to prevent racing conditions.
            eventsSupportedByState = eventsSupportedByState_;

            return eventsSupportedByState_[state];
        }
    }

    private void PopulateStatesWithParents()
    {
        State<TState>[] states = States;
        int length = states.Length;
        foreach (State<TState> element in states)
        {
            State<TState> element_ = element;
            while (element_.TryGetParentState(out int parent))
            {
                length++;
                element_ = States[parent];
            }
        }

        ReadOnlySlice<TState>[] statesWithParents_ = new ReadOnlySlice<TState>[states.Length];
        TState[] statesForSlice = new TState[length];
        int j = 0;
        for (int i = 0; i < States.Length; i++)
        {
            int l = 0;
            int current = i;
            State<TState> element;
            do
            {
                element = States[current];
                statesForSlice[j++] = element.state;
                l++;
            }
            while (element.TryGetParentState(out current));
            statesWithParents_[i] = new ReadOnlySlice<TState>(statesForSlice, i, l);
        }

        // We assing the field at the end to prevent racing conditions.
        Interlocked.CompareExchange(ref statesWithParents, statesWithParents_, null);
    }

    private void PopulateEventsSupportedByStateIndex()
    {
        Dictionary<(int State, TEvent Event), int> transitionStartIndexes = TransitionStartIndexes;
        TEvent[] events = new TEvent[transitionStartIndexes.Count];
        int length = States.Length;
        ReadOnlySlice<TEvent>[] eventsSupportedByStateIndex_ = new ReadOnlySlice<TEvent>[length];

        // TODO: Allocations could be reduced by flatting this.
        List<TEvent>[] tmp = new List<TEvent>[length];
        // Don't use .Keys because it allocates more memory.
        foreach (var kv in transitionStartIndexes)
        {
            List<TEvent>? list = tmp[kv.Key.State];
            if (list is null)
                tmp[kv.Key.State] = list = new List<TEvent>();
            list.Add(kv.Key.Event);
        }

        int j = 0;
        for (int i = 0; i < length; i++)
        {
            if (tmp[i] is List<TEvent> list)
            {
                eventsSupportedByStateIndex_[i] = new ReadOnlySlice<TEvent>(events, j, list.Count);
                foreach (TEvent @event in list)
                    events[j++] = @event;
            }
        }

        // We assing the field at the end to prevent racing conditions.
        Interlocked.CompareExchange(ref eventsSupportedByStateIndex, eventsSupportedByStateIndex_, null);
    }
}
