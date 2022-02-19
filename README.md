![.NET Core](https://github.com/Enderlook/Net-State-Machine/workflows/.NET%20Core/badge.svg?branch=master)

# .NET State Machine

An state machine builder library for .NET.

The following example show all the functions of the state machine (except overloads which are described bellow):

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
            // Parameter is generic so it doesn't allocate of value types.
            // This parameter is passed to subscribed delegate which accepts the generic argument type in it's signature.
            // If you don't want to pass a parameter you can use Update().
            // Alternatively, if you want to pass multiple parameters you can use UpdateWithParameters().With(param1).With(param2).Done().
            // This parameter system can also be used with fire event methods.
            character.stateMachine.UpdateWithParameter(character.rnd.NextSingle());

            Console.WriteLine($"State: {character.stateMachine.CurrentState}.");
            Console.WriteLine($"Health: {character.health}.");
            Console.WriteLine($"Food: {character.food}.");

            await Task.Delay(10).ConfigureAwait(false);
        }
    }

    public Character() => stateMachine = GetStateMachineFactory().Create(this);

    private static StateMachineFactory<States, Events, Character> GetStateMachineFactory()
    {
        if (factory is not null)
            return factory;

        StateMachineFactory<States, Events, Character>? factory_ =  StateMachine<States, Events, Character>
            // State machines are created from factories which makes the creations of multiple instances
            // cheaper in both CPU and memory since computation is done once and shared between created instances.
            .CreateFactoryBuilder()
            // Determines the initial state of the state machine.
            // false specify that we don't want to run the OnEntry delegates of the Sleep state during the initialization of the state machine.
            .SetInitialState(States.Sleep, false)
            // Configures an state.
            .In(States.Sleep)
                // Executed every time we enter to this state.
                .OnEntry(() => Console.WriteLine("Going to bed."))
                // Executed every time we exit from this state.
                .OnExit(() => Console.WriteLine("Getting up."))
                // Executed every time update method (either Update(), UpdateWithParameter<T>(T) or UpdateWithParameters().[...].Done() is executed and is in this state.
                // All events provide an overload to pass a recipient, so it can be parametized during build of concrete instances.
                // Also provides an overload to pass a parameter of arbitrary type, so it can be parametized durin call of UpdateWithParameter<T>(T) or UpdateWithParameters().With<T>(param).Done().
                // Also provides an overload to pass both a recipient and a parameter of arbitrary type.
                // This overloads also applies to OnEntry(...), OnExit(...), If(...) and Do(...) methods.
                .OnUpdate(@this => @this.OnUpdateSleep())
                .On(Events.HasFullHealth)
                    // Executed every time this event is fired in this state.
                    .Do(() => Console.WriteLine("Pick toys."))
                    // New state to transite.
                    .Goto(States.Play)
                .On(Events.IsHungry)
                    // Only execute the next call if the condition is true.
                    .If(@this => @this.IsVeryWounded())
                        // We stay in our current state without executing OnEntry nor OnExit delegates.
                        .StaySelf()
                    // If we wanted to execute those delegates we can use:
                    //  .Goto(States.Sleep)
                    // Or:
                    //  .GotoSelf()
                    // Else execute the next call if the condition is true.
                    .If(@this => @this.IsWounded())
                        .Goto(States.Gather)
                    // Else execute unconditionally.
                    .Goto(States.Hunt)
                // Ignore this event in this transition.
                // (If we don't add this and we accidentally fire this event an exception is thrown).
                .Ignore(Events.LowHealth)
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
                // This means that OnUpdate delegates in the parent state will also be run,
                // moreover the parent's OnEntry and OnExit delegates will only be run if entering or exiting the parent state,
                // (that means, they are not run if transiting between different substates of the same parent state).
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

The following methods have overloads:

`Fire(TEvent)` and `Fire(TEvent, TParameter)`. The method accept an optional parameter `TParameter` which is send to every delegate. If `TParameter` is missing, `default` is used.

`ExecuteOnEntry(Action)`, `ExecuteOnExit(Action)`, `ExecuteOnUpdate(Action)` and `Execute(Action)` has an additional overload: `ExecuteOnEntry(Action<TParameter>)`, `ExecuteOnExit(Action<TParameter>)`, `ExecuteOnUpdate(Action<TParameter>)` and `Execute(Action<TParameter>)`, where `TParameter` parameter is the value passed on `.Fire(TEvent, TParameter)`.

The parameter `TParameter` is optional, and can be ignored.
For example: 
 - If you use `Fire(TEvent)` and `Execute(Action<TParameter>)`, `TParameter` is replaced by `default`.
 - If you use `Fire(TEvent, TParameter)` but `Execute(Action)`, `TParameter` is ignored.

`If(Func<bool>)` also have `If(Func<TParameter, bool)`.

`Start()` also have `Start(TParameter)`, where `TParameter` is used in `ExecuteOnEntry(Action<TParameter>)` of the initial state.

We added the generic parameter `TParameter` instead of using a simple `Object` type so you can specify custom constraints on it. Also this allows you to remove boxing when passing structs.

# Changelog

## 0.1.1
- Fix `.GotoSelf()` requiring an state and producing `StackOverflowException`.
- Fix documentation references.
- Turn `HasSubTransitions` and `GetGoto` methods in `TransitionBuilder<TState, TEvent>` from `protected` to `private protected`.

## 0.2.0
- Fix documentation references.
- Add `TParameter` on `StateMachine` and other classes to specify a common ground type for event parameters.
- Rename `.Execute(...)` to `.Do(...)`, `ExecuteOnEntry(...)` to `OnEntry(...)`, `ExecuteOnExit(...)` to `OnExit(...)` and `ExecuteOnUpdate(...)` to `OnUpadate(...)` for more fluent API.
- Support multiple calls to delegate subscription methods instead of throwing `InvalidOperationException` on the second call.
- Improve perfomance by passing internal large structs by reference.
- Turn `StateMachine<TState, TEvent, TParameter>`, `StateMachineBuilder<TState, TEvent, TParameter>`, `StateBuilder<TState, TEvent, TParameter>`, `SlaveTransitionBuilder<TState, TEvent, TParameter, TParent>` and `MasterTransitionBuilder<TState, TEvent, TParameter>` into `sealed`.

## 0.2.1
- Remove `IComparable` constraint in `TState` and `TEvent` generic parameters.

## 0.3.0
- Add nullable reference analysis.
- Add support for storing a recipient and decoupling parameters from type signature (turn `StateMachine<TState, TEvent, TParameter>` into `StateMachine<TState, TEvent, TRecipient>`, and adding additional and renaming fire and update methods).
- Rework entire builder API to simplify and use factory pattern:
  - Turn `StateMachineBuilder<TState, TEvent, TParameter>` to `StateMachineBuilder<TState, TEvent, TRecipient>`.
  - Add `StateMachineFactory<TState, TEvent, TRecipient>`.
  - Replace `Build` method with `Finalize` in `StateMachineBuilder<TState, TEvent, TRecipient>`.
  - Replace `TransitionBuilder<TState, TEvent, TRecipient>`, `MasterTransitionBuilder<TState, TEvent, TParameter>` and `SlaveTransitionBuilder<TState, TEvent, TParameter, TParent>` with `TransitionBuilder<TState, TEvent, TRecipient, TParent>`.
  - Turn `StateBuilder<TState, TEvent, TParameter>` into `StateBuilder<TState, TEvent, TRecipient>`.
  - Add additional overloads to subscribers that accepts `TRecipient` and/or `TParameter` parameters.
- Replace `State` property in `StateMachine<TState, TEvent, TRecipient>` with `CurrentState`.
- Replace `Build` method in `StateMachine<TState, TEvent, TRecipient>` with `CreateFactoryBuilder`.
- Add properties `CurrentStateHierarchy` and `CurrentAcceptedEvents`, and methods `GetParentStateOf`, `GetParentHierarchyOf`, `GetAcceptedEventsBy`, `IsInState` to `StateMachine<TState, TEvent, TRecipient>`.
- Add proper support for recursive fire calls.

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
    
    /// Same as Fire(TEvent) but includes a value that can be passed to subscribed delegates.
    public void FireWithParameter<TParameter>(TEvent @event, TParameter parameter);
    
    /// Same as Fire(TEvent) but returns a builder that can accept arbitrary amount of parameter that can be passed to subscribed delegates.
    public FireParametersBuilder FireWithParameters(TEvent @event);
    
    /// Fire an event to the state machine.
    /// The event won't be enqueued but actually run, ignoring previously enqueued events.
    /// If subsequent events are enqueued during the execution of the callbacks of this event, they will also be run after the completion of this event.
    public void FireImmediately(TEvent @event);
    
    /// Same as FireImmediately(TEvent) but includes a value that can be passed to subscribed delegates.
    public void FireImmediatelyWithParameter<TParameter>(TEvent @event, TParameter parameter);
    
    /// Same as FireImmediately(TEvent) but returns a builder that can accept arbitrary amount of parameter that can be passed to subscribed delegates.
    public FireImmediatelyParametersBuilder FireImmediatelyWithParameters(TEvent @event);
    
    /// Executes the update callbacks registered in the current state.
    public void Update();
    
    /// Same as Update() but includes a value that can be passed to subscribed delegates.
    public void UpdateWithParameter<TParameter>(TParameter parameter);
    
    /// Same as Update() but returns a builder that can accept arbitrary amount of parameter that can be passed to subscribed delegates.
    public UpdateParametersBuilder UpdateWithParameters();

    public readonly struct FireParametersBuilder 
    {
        /// Stores a parameter tha can be passed to callbacks.
        public FireParametersBuilder With<TParameter>(TParameter parameter);
        
        /// Fires the event.
        public void Done();
    }
    
    public readonly struct FireImmediatelyParametersBuilder
    {
        /// Stores a parameter tha can be passed to callbacks.
        public FireImmediatelyParametersBuilder With<TParameter>(TParameter parameter);
        
        /// Fires the event.
        public void Done();
    }
    
    public readonly struct UpdateParametersBuilder
    {
        /// Stores a parameter tha can be passed to callbacks.
        public UpdateParametersBuilder With<TParameter>(TParameter parameter);
        
        /// Executes the update.
        public void Done();
    }
    
    public readonly struct CreateParametersBuilder
    {
        /// Stores a parameter tha can be passed to callbacks.
        public CreateParametersBuilder With<TParameter>(TParameter parameter);
        
        /// Inialized the state machine.
        public StateMachine<TState, TEvent, TRecipient> Done();
    }    
}

public sealed class StateMachineFactory<TState, TEvent, TRecipient>
    where TState : notnull
    where TEvent : notnull
{
    /// Creates a configured and initialized state machine using the configuration provided by this factory.
    public StateMachine<TState, TEvent, TRecipient> Create(TRecipient recipient);
    
    /// Same as Create() but includes a value that can be passed to subscribed delegates.
    public StateMachine<TState, TEvent, TRecipient> CreateWithParameter<TParameter>(TRecipient recipient, TParameter parameter)
    
    /// Same as Create() but returns a builder that can accept arbitrary amount of parameter that can be passed to subscribed delegates.
    public StateMachine<TState, TEvent, TRecipient>.CreateParametersBuilder CreateWithParameters(TRecipient recipient)
}

public sealed class StateMachineBuilder<TState, TEvent, TRecipient> : IFinalizable
    where TState : notnull
    where TEvent : notnull
{
    /// Determines the initial state of the state machine.
    /// If `runEntryActions` is true, subscribed delegates to the OnEntry ovents of the specified state will be run during the initialization of the state machine.
    public StateMachineBuilder<TState, TEvent, TRecipient> SetInitialState(TState state, bool runEntryActions = true);
    
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
    
    /// Determines to which state this transition goes.
    public TParent Goto(TState state);
    
    /// Determines to transite to the current state.
    /// That means, that OnExit and OnEntry actions of current state (but not parent states in case of current state being a substate) will be executed.
    public TParent GotoSelf();
    
    /// Determines that will have no transition to any state, so no OnEntry nor OnExit event will be raised.
    public TParent StaySelf();
}
```