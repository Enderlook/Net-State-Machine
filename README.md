![.NET Core](https://github.com/Enderlook/Net-State-Machine/workflows/.NET%20Core/badge.svg?branch=master)

# .NET State Machine

An state machine builder library for .NET.

The following example shows some the functions of the state machine.

```cs
using Enderlook.StateMachine;

public class Character
{
    private static StateMachineFactory<States, Events, Character>? factory;

    private readonly Random rnd = new();
    private readonly StateMachine<States, Events, Character> stateMachine;
    private int health = 100;
    private int food = 100;

    private enum States
    {
        Sleep,
        Play,
        GettingFood,
        Hunt,
        Gather,
    }

    private enum Events
    {
        HasFullHealth,
        LowHealth,
        IsHungry,
        IsNoLongerHungry,
    }

    public static async Task Main()
    {
        Character character = new();
        while (true)
        {
            Console.Clear();

            // Executes an update call of the state machine and pass an arbitrary parameter to it.
            // Parameter is generic so it doesn't allocate on value types.
            // This parameter is passed to subscribed delegate which accepts the generic argument type in it's signature.
            // If you don't want to pass a parameter you can remove the .With() method call.
            // This parameter system can also be used with fire event methods.
            character.stateMachine.With(character.rnd.NextSingle()).Update();

            Console.WriteLine($"State: {character.stateMachine.CurrentState}.");
            Console.WriteLine($"Health: {character.health}.");
            Console.WriteLine($"Food: {character.food}.");

            await Task.Delay(10).ConfigureAwait(false);
        }
    }

    public Character()
    {
        // Creates an instance of the state machine.
        stateMachine = GetStateMachineFactory().Create(this);
     // Alternatively if you want to pass parameters to the initialization of the state machine you can do:
     // stateMachine = GetStateMachineFactory().With(parameter).Create(this).
	 // The method `.With(parameter)` can be concatenated as many times you need.
	 // The pattern `stateMachine.With(p1).With(p2)...With(pn).SomeMethod(...)` is also valid for methods `Fire()`, `FireImmediately()` and `Update()`.
    }

    private static StateMachineFactory<States, Events, Character> GetStateMachineFactory()
    {
        if (factory is not null)
            return factory;

        StateMachineFactory<States, Events, Character>? factory_ = StateMachine<States, Events, Character>
            // State machines are created from factories which makes the creations of multiple instances
            // cheaper in both CPU and memory since computation is done once and shared between created instances.
            .CreateFactoryBuilder()
            // Determines the initial state of the state machine.
            // The second parameter determines how OnEntry delegates should be executed during the initialization of the state machine,
            // InitializationPolicy.Ignore means they should not be run.
            .SetInitialState(States.Sleep, InitializationPolicy.Ignore)
            // Configures an state.
            .In(States.Sleep)
                // Executed every time we enter to this state.
                .OnEntry(() => Console.WriteLine("Going to bed."))
                // Executed every time we exit from this state.
                .OnExit(() => Console.WriteLine("Getting up."))
                // Executed every time update method (either Update() or With<T>(T).Update()) is executed and is in this state.
                // All events provide an overload to pass a recipient, so it can be parametized during build of concrete instances.
                // Also provides an overload to pass a parameter of arbitrary type, so it can be parametized during call of With<T>(T).Update().
                // Also provides an overload to pass both a recipient and a parameter of arbitrary type.
                // This overloads also applies to OnEntry(...), OnExit(...), If(...) and Do(...) methods.
                .OnUpdate(@this => @this.OnUpdateSleep())
                .On(Events.HasFullHealth)
                    // Executed every time this event is fired in this state.
                    .Do(() => Console.WriteLine("Pick toys."))
                    // New state to transite.
                    .Goto(States.Play)
                // Alternatively, you can configure the event execution policy during the transition.
                // The above method call is equivalent to:
                // .OnEntryPolicy(TransitionPolicy.ChildFirstWithCulling).OnExitPolicy(TransitionPolicy.ParentFirstWithCulling).Goto(States.Play).
                .On(Events.IsHungry)
                    // Only execute the next call if the condition is true.
                    .If(@this => @this.IsVeryWounded())
                        // We stay in our current state without executing OnEntry nor OnExit delegates.
                        .StaySelf()
                    // The above method is a shortcut of:
                    //  .OnEntryPolicy(TransitionPolicy.Ignore).OnExitPolicy(TransitionPolicy.Ignore).Goto(States.Sleep).
                    // If we wanted to execute those delegates we can use:
                    //  .GotoSelf(false)
                    // Which is the shortcut of:
                    //  .OnEntryPolicy(TransitionPolicy.ChildFirstWithCullingInclusive).OnExitPolicy(TransitionPolicy.ParentFirstWithCullingInclusive).Goto(States.Sleep).
                    // If additionally, we wanted to execute transition delegates from its parents states (something which is not useful in this example since State.Sleep is not a substate) we can do:
                    //  .GotoSelf(true)
                    // Which is the shortcut of:
                    //  .OnEntryPolicy(TransitionPolicy.ChildFirst).OnExitPolicy(TransitionPolicy.ParentFirst).Goto(States.Sleep).
                    // Else execute the next call if the condition is true.
                    .If(@this => @this.IsWounded())
                        .Goto(States.Gather)
                    // Else execute unconditionally.
                    .Goto(States.Hunt)
                // Ignore this event in this transition.
                // (If we don't add this and we accidentally fire this event an exception is thrown).
                .Ignore(Events.LowHealth)
            // Which is the shortcut of:
            //  .On(Events.LowHealth).OnEntryPolicy(TransitionPolicy.Ignore).OnExitPolicy(TransitionPolicy.Ignore).Goto(States.Sleep).
            .In(States.Play)
                .OnUpdate(@this => @this.OnUpdatePlay())
                .On(Events.IsHungry)
                    .If(@this => @this.IsWounded())
                        .Goto(States.Gather)
                    .Goto(States.Hunt)
            .In(States.GettingFood)
                .OnEntry(() => Console.WriteLine("Going for food."))
                .OnExit(() => Console.WriteLine("Stop going for food."))
            .In(States.Gather)
                // Determines that this state is a substate of another.
                // This means that OnUpdate delegates in the parent state will also be run.
                // Also depending on the configured OnEntryPolicy and OnExitPolicy during transitions,
                // the OnEntry and OnExit delegates subscribted in this state may be run during transitions in substates.
                .IsSubStateOf(States.GettingFood)
                .OnUpdate((Character @this, float parameter) => @this.OnUpdateGather(parameter))
                .On(Events.IsNoLongerHungry)
                    .If(@this => @this.IsWounded())
                        .Goto(States.Sleep)
                    .Goto(States.Play)
                .On(Events.HasFullHealth)
                    .Goto(States.Hunt)
            .In(States.Hunt)
                .IsSubStateOf(States.GettingFood)
                .OnEntry(() => Console.WriteLine("Take bow."))
                .OnExit(() => Console.WriteLine("Drop bow."))
                .OnUpdate((Character @this, float parameter) => @this.OnUpdateHunt(parameter))
                .On(Events.IsNoLongerHungry)
                    .Goto(States.Sleep)
                .On(Events.LowHealth)
                    .Goto(States.Sleep)
            .Finalize();

        // The interlocked is useful to reduce memory usage in multithreading situations.
        // That is because the factory contains common data between instances,
        // so if two instances are created from two different factories it will consume more memory
        // than two instances created from the same factory.
        Interlocked.CompareExchange(ref factory, factory_, null);
        return factory;
    }

    private bool IsVeryWounded() => health <= 50;

    private bool IsWounded() => health <= 75;

    private void OnUpdateHunt(float luck)
    {
        food += (int)MathF.Round(rnd.Next(8) * luck);
        if (food >= 100)
        {
            food = 100;
            stateMachine.Fire(Events.IsNoLongerHungry);
         // Alternatively if you want to pass parameters to the initialization of the state machine you can do:
		 // stateMachine.With(paramter).Fire(Events.IsNoLongerHungry);
        }

        health -= (int)MathF.Round(rnd.Next(6) * (1 - luck));
        if (health <= 20)
            stateMachine.Fire(Events.LowHealth);
    }

    private void OnUpdateGather(float luck)
    {
        food += (int)MathF.Round(rnd.Next(3) * luck);
        if (food >= 100)
        {
            food = 100;
            stateMachine.Fire(Events.IsNoLongerHungry);
        }

        if (rnd.Next(1) % 1 == 0)
        {
            health++;
            if (health >= 100)
            {
                health = 100;
                stateMachine.Fire(Events.HasFullHealth);
            }
        }
    }

    private void OnUpdatePlay()
    {
        food -= 3;
        if (food <= 0)
        {
            food = 0;
            stateMachine.Fire(Events.IsHungry);
        }
    }

    private void OnUpdateSleep()
    {
        health++;
        if (health >= 100)
        {
            health = 100;
            stateMachine.Fire(Events.HasFullHealth);
        }

        food -= 2;
        if (food <= 0)
        {
            food = 0;
            stateMachine.Fire(Events.IsHungry);
        }
    }
}
```

# API

```cs
public sealed class StateMachine<TState, TEvent, TRecipient>
    where TState : notnull
    where TEvent : notnull
{
    /// Get current (sub)state of this state machine.
    public TState CurrentState { get; }

    /// Get current (sub)state and all its parent state hierarchy.
    public ReadOnlySlice<TState> CurrentStateHierarchy  { get; }

    /// Get accepts events by current (sub)state.
    public ReadOnlySlice<TEvent> CurrentAcceptedEvents { get; }

    /// Creates a factory builder.
    public static StateMachineBuilder<TState, TEvent, TRecipient> CreateFactoryBuilder();

    /// Get the parent state of the specified state.
    /// If state is not a substate, returns false.
    public bool GetParentStateOf(TState state, [NotNullWhen(true)] out TState? parentState);

    /// Get the parent hierarchy of the specified state. If state is not a substate, returns empty.
    public ReadOnlySlice<TState> GetParentHierarchyOf(TState state);

    /// Get the events that are accepted by the specified state.
    public ReadOnlySlice<TEvent> GetAcceptedEventsBy(TState state);

    /// Determines if the current state is the specified state or a (nested) substate of that specified state.
    public bool IsInState(TState state);

    /// Fire an event to the state machine.
    /// If the state machine is already firing an state, it's enqueued to run after completion of the current event.
    public void Fire(TEvent @event);

    /// Fire an event to the state machine.
    /// The event won't be enqueued but actually run, ignoring previously enqueued events.
    /// If subsequent events are enqueued during the execution of the callbacks of this event, they will also be run after the completion of this event.
    public void FireImmediately(TEvent @event);

    /// Executes the update callbacks registered in the current state.
    public void Update();
    
    /// Stores a parameter(s) that can be passed to subscribed delegates.
    public ParametersBuilder With<T>(T parameter);

    public readonly struct ParametersBuilder
    {
        /// Stores a parameter tha can be passed to callbacks.
        public ParametersBuilder With<TParameter>(TParameter parameter);

        /// Same as Fire(TEvent) in parent class but includes all the stored value that can be passed to subscribed delegates.
        public void Fire(TEvent);
        
        /// Same as FireImmediately(TEvent) in parent class but includes all the stored value that can be passed to subscribed delegates.
        public void FireImmediately(TEvent);
        
        /// Same as Update(TEvent) in parent class but includes all the stored value that can be passed to subscribed delegates.
        public void Update(TEvent);
    }	

    public readonly struct InitializeParametersBuilder
    {
        /// Stores a parameter tha can be passed to callbacks.
        public InitializeParametersBuilder With<TParameter>(TParameter parameter);

        /// Creates the state machine.
        public StateMachine<TState, TEvent, TRecipient> Create(TRecipient recipient);
    }
}

public sealed class StateMachineFactory<TState, TEvent, TRecipient>
    where TState : notnull
    where TEvent : notnull
{
    /// Creates a configured and initialized state machine using the configuration provided by this factory.
    public StateMachine<TState, TEvent, TRecipient> Create(TRecipient recipient);
    
    /// Stores a parameter(s) that can be passed to subscribed delegates.
    public StateMachine<TState, TEvent, TRecipient>.InitializeParametersBuilder With<T>(T parameter);
}

public sealed class StateMachineBuilder<TState, TEvent, TRecipient> : IFinalizable
    where TState : notnull
    where TEvent : notnull
{
    /// Determines the initial state of the state machine.
    /// `initializationPolicy` determines how subscribed delegates to the OnEntry ovents of the specified state (and parent states) will be run during the initialization of the state machine.
    public StateMachineBuilder<TState, TEvent, TRecipient> SetInitialState(TState state, ExecutionPolicy initializationPolicy = ExecutionPolicy.ChildFirst);

    ///  Add a new state or loads a previously added state.
    public StateBuilder<TState, TEvent, TRecipient> In(TState state);

    /// Creates a factory from using as configuration the builder.
    public StateMachineFactory<TState, TEvent, TRecipient> Finalize();
}

public sealed class StateBuilder<TState, TEvent, TRecipient> : IFinalizable
    where TState : notnull
    where TEvent : notnull
{
    /// Fowards call to StateMachineBuilder<TState, TEvent, TRecipient>.In(TState state).
    public StateBuilder<TState, TEvent, TRecipient> In(TState state);

    /// Fowards call to StateMachineBuilder<TState, TEvent, TRecipient>.Finalize();
    public StateMachineFactory<TState, TEvent, TRecipient> Finalize();

    /// Marks this state as the substate of the specified state.
    public StateBuilder<TState, TEvent, TRecipient> IsSubStateOf(TState state);

    /// Determines an action to execute on entry to this state.
    public StateBuilder<TState, TEvent, TRecipient> OnEntry(Action action);

    /// Same as OnEntry(Action) but pass the recipient as parameter.
    public StateBuilder<TState, TEvent, TRecipient> OnEntry(Action<TRecipient> action);

    /// Same as OnEntry(Action) but pass to the delegate any parameter passed during the call which matches the generic parameter type.
    /// If no parameter passed with the specified generic parameter is found, it's ignored.
    public StateBuilder<TState, TEvent, TRecipient> OnEntry<TParameter>(Action<TParameter> action);

    /// Combined version of OnEntry(Action<TRecipient>) and OnEntry(Action<TParameter>).
    public StateBuilder<TState, TEvent, TRecipient> OnEntry<TParameter>(Action<TRecipient, TParameter> action);

    /// Determines an action to execute on exit fropm this state.
    public StateBuilder<TState, TEvent, TRecipient> OnExit(Action action);

    /// Same as OnExit(Action) but pass the recipient as parameter.
    public StateBuilder<TState, TEvent, TRecipient> OnExit(Action<TRecipient> action);

    /// Same as OnExit(Action) but pass to the delegate any parameter passed during the call which matches the generic parameter type.
    /// If no parameter passed with the specified generic parameter is found, it's ignored.
    public StateBuilder<TState, TEvent, TRecipient> OnExit<TParameter>(Action<TParameter> action);

    /// Combined version of OnExit(Action<TRecipient>) and OnExit(Action<TParameter>).
    public StateBuilder<TState, TEvent, TRecipient> OnExit<TParameter>(Action<TRecipient, TParameter> action);

    /// Determines an action to execute on update to this state.
    public StateBuilder<TState, TEvent, TRecipient> OnUpdate(Action action);

    /// Same as OnUpdate(Action) but pass the recipient as parameter.
    public StateBuilder<TState, TEvent, TRecipient> OnUpdate(Action<TRecipient> action);

    /// Same as OnUpdate(Action) but pass to the delegate any parameter passed during the call which matches the generic parameter type.
    public StateBuilder<TState, TEvent, TRecipient> OnUpdate<TParameter>(Action<TParameter> action);

    /// Combined version of OnUpdate(Action<TRecipient>) and OnUpdate(Action<TParameter>).
    /// If no parameter passed with the specified generic parameter is found, it's ignored.
    public StateBuilder<TState, TEvent, TRecipient> OnUpdate<TParameter>(Action<TRecipient, TParameter> action);

    /// Add a behaviour that is executed during the firing of the specified event.
    public TransitionBuilder<TState, TEvent, TRecipient, StateBuilder<TState, TEvent, TRecipient>> On(TEvent @event);

    /// Ignores the specified event.
    /// If no behaviour is added to an event and it's fired, it will throw. This prevent throwing by ignoring the call at all.
    public StateBuilder<TState, TEvent, TRecipient> Ignore(TEvent @event);
}

public sealed class TransitionBuilder<TState, TEvent, TRecipient, TParent> : IFinalizable, ITransitionBuilder<TState>
    where TState : notnull
    where TEvent : notnull
{
    /// Add a sub transition which is executed when the delegate returns true.
    public TransitionBuilder<TState, TEvent, TRecipient, TransitionBuilder<TState, TEvent, TRecipient, TParent>> If(Func<bool> guard);

    /// Same as If(Func<bool>) but pass the recipient as parameter.
    public TransitionBuilder<TState, TEvent, TRecipient, TransitionBuilder<TState, TEvent, TRecipient, TParent>> If(Func<TRecipient, bool> guard);

    /// Same as If(Func<bool>)  but pass to the delegate any parameter passed during the call which matches the generic parameter type.
    public TransitionBuilder<TState, TEvent, TRecipient, TransitionBuilder<TState, TEvent, TRecipient, TParent>> If<TParameter>(Func<TParameter, bool> guard);

    /// Combined version of If(Func<TRecipient, bool>) and If(Func<TParameter, bool>).
    public TransitionBuilder<TState, TEvent, TRecipient, TransitionBuilder<TState, TEvent, TRecipient, TParent>> If<TParameter>(Func<TParameter, bool> guard);

    /// Determines an action to execute when the event is raised.
    public TransitionBuilder<TState, TEvent, TRecipient, TParent> Do(Action action);

    /// Same as Do(Action) but pass the recipient as parameter.
    public TransitionBuilder<TState, TEvent, TRecipient, TParent> Do(Action<TRecipient> action);

    /// Same as Do(Action) but pass to the delegate any parameter passed during the call which matches the generic parameter type.
    /// If no parameter passed with the specified generic parameter is found, it's ignored.
    public TransitionBuilder<TState, TEvent, TRecipient, TParent> Do<TParameter>(Action<TParameter> action);

    /// Combined version of Do(Action<TRecipient>) and Do(Action<TParameter>).
    public TransitionBuilder<TState, TEvent, TRecipient, TParent> Do<TParameter>(Action<TRecipient, TParameter> action);

    ///  Configures the policy of how subscribed delegates to on entry hook should be executed.
    /// If this method is not executed, the default policy is TransitionPolicy.ParentFirstWithCulling.
    public GotoBuilder<TState, TEvent, TRecipient, TParent> OnEntryPolicy(TransitionPolicy policy);
    
    ///  Configures the policy of how subscribed delegates to on exit hook should be executed.
    /// If this method is not executed, the default policy is TransitionPolicy.ChildFirstWithCulling.
    public GotoBuilder<TState, TEvent, TRecipient, TParent> OnExitPolicy(TransitionPolicy policy);

    /// Determines to which state this transition goes.
    /// This is equivalent to: OnEntryPolicy(TransitionPolicy.ChildFirstWithCulling).OnExitPolicy(TransitionPolicy.ParentFirstWithCulling).Goto(state).
    public TParent Goto(TState state);

    /// Determines to transite to the current state.
    /// If runParentsActions is true: OnExit and OnEntry actions of current state (but not parent states in case of current state being a substate) will be executed.
    /// This is equivalent to OnEntryPolicy(TransitionPolicy.ChildFirstWithCullingInclusive).OnExitPolicy(TransitionPolicy.ParentFirstWithCullingInclusive).Goto(currentState).
    /// If runParentActions is false: OnExit and OEntry actions of the current state (and parents in case of current state being a substate) will be executed.
    /// This is equivalent to OnEntryPolicy(TransitionPolicy.ChildFirst).OnExitPolicy(TransitionPolicy.ParentFirst).Goto(currentState).
    public TParent GotoSelf(bool runParentsActions = false);

    /// Determines that will have no transition to any state, so no OnEntry nor OnExit event will be raised.
    /// This is equivalent to OnEntryPolicy(TransitionPolicy.Ignore).OnExitPolicy(TransitionPolicy.Ignore).GotoSelf().
    public TParent StaySelf();
}

public sealed class GotoBuilder<TState, TEvent, TRecipient, TParent> : IGoto<TState>
    where TState : notnull
    where TEvent : notnull
{
    /// Configures the policy of how subscribed delegates to on entry hook should be executed.
    /// If this method is not executed, the default policy is TransitionPolicy.ParentFirstWithCulling.
    public GotoBuilder<TState, TEvent, TRecipient, TParent> OnEntryPolicy(TransitionPolicy policy);
    
    /// Configures the policy of how subscribed delegates to on exit hook should be executed.
    /// If this method is not executed, the default policy is TransitionPolicy.ChildrenFirstWithCulling.
    public GotoBuilder<TState, TEvent, TRecipient, TParent> OnExitPolicy(TransitionPolicy policy);

    /// Determines to which state this transition goes.
    public TParent Goto(TState state);
    
    /// Determines to transite to the current state.
    /// This is a shortcut of Goto(currentState).
    public TParent GotoSelf();
}

/// Determines the transition policy between two states.
/// This configures how subscribed delegates on states are run during transition between states.
public enum TransitionPolicy
{
    /// Determines that subscribed delegates should not run.
    Ignore = 0,

    /// Determines that subscribed delegates on parents are run first.
    ParentFirst = 1,

    /// Determines that subscribed delegates on children are run first.
    ChildFirst = 2,

    /// Determines that subscribed delegates on parents are run first from (excluding) the last common parent between the two states.
    ParentFirstWithCulling = 3,

    /// Determines that subscribed delegates on children are run first until reach (excluding) the last common parent between the two states.
    ChildFirstWithCulling = 4,

    /// Determines that subscribed delegates on parents are run first from (including) the last common parent between the two states.
    ParentFirstWithCullingInclusive = 5,

    /// Determines that subscribed delegates on children are run first until reach (including) the last common parent between the two states.
    ChildFirstWithCullingInclusive = 6,
}

/// Represent an slice of data.
public readonly struct ReadOnlySlice<T> : IReadOnlyList<T>
{
    /// Get the element specified at the index.
    public T this[int index] { get; }

    /// Get the count of the slice.
    public int Count { get; }

    /// Get an <see cref="ReadOnlyMemory{T}"/> of this slice.
    public ReadOnlyMemory<T> Memory { get; }

    /// Get an <see cref="ReadOnlySpan{T}"/> of this slice.
    public ReadOnlySpan<T> Span { get; }

    /// Get the enumerator of the slice.
    public Enumerator GetEnumerator();

    /// Enumerator of <see cref="ReadOnlySlice{T}"/>.
    public struct Enumerator : IEnumerator<T>
    {
        /// Get current element of the enumerator.
        public T Current { get; }

        /// Moves to the next element of the enumeration.
        public bool MoveNext();

        /// Reset the enumeration.
        public void Reset();
    }
}
```
