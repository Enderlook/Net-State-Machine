# Changelog

## 0.4.0
- Add support to customize transition policies.
- Modify following APIs:
```diff
public partial sealed class StateMachine<TState, TEvent, TRecipient>
{
-   public void FireWithParameter<TParameter>(TEvent @event, TParameter parameter);
-   public FireParametersBuilder FireWithParameters(TEvent @event);
-   public FireImmediatelyParametersBuilder FireImmediatelyWithParameters(TEvent @event);
-   public void FireImmediatelyWithParameter<TParameter>(TEvent @event, TParameter parameter);
-   public void UpdateWithParameter<TParameter>(TParameter parameter);
-   public UpdateParametersBuilder UpdateWithParameters();
+   public ParametersBuilder With<T>(T parameter);

-   public readonly struct FireParametersBuilder
-   {
-       public FireParametersBuilder With<TParameter>(TParameter parameter);
-       public void Done();
-   }

-   public readonly struct FireImmediatelyParametersBuilder
-   {
-       public FireImmediatelyParametersBuilder With<TParameter>(TParameter parameter);
-       public void Done();
-   }

-   public readonly struct UpdateParametersBuilder
-   {
-       public UpdateParametersBuilder With<TParameter>(TParameter parameter);
-       public void Done();
-   }

-   public readonly struct CreateParametersBuilder
-   {
-       public CreateParametersBuilder With<TParameter>(TParameter parameter);
-       public StateMachine<TState, TEvent, TRecipient> Done();
-   }

+   public readonly struct ParametersBuilder
+   {
+       public ParametersBuilder With<TParameter>(TParameter parameter);
+       public void Fire(TEvent);
+       public void FireImmediately(TEvent);
+       public void Update(TEvent);
+   }

+   public readonly struct InitializeParametersBuilder
+   {
+       public InitializeParametersBuilder With<TParameter>(TParameter parameter);
+       public StateMachine<TState, TEvent, TRecipient> Create(TRecipient recipient);
+   }
}

public sealed class StateMachineFactory<TState, TEvent, TRecipient>
{
-   public StateMachine<TState, TEvent, TRecipient> CreateWithParameter<TParameter>(TRecipient recipient, TParameter parameter);    
-   public StateMachine<TState, TEvent, TRecipient>.CreateParametersBuilder CreateWithParameters(TRecipient recipient);

+   public StateMachine<TState, TEvent, TRecipient>.InitializeParametersBuilder With<T>(T parameter);
}

public sealed partial class TransitionBuilder<TState, TEvent, TRecipient, TParent> : IFinalizable, ITransitionBuilder<TState>
{
-   public TParent GotoSelf();
+   public TParent GotoSelf(bool runEntryActions);
+   public GotoBuilder<TState, TEvent, TRecipient, TParent> OnEntryPolicy(TransitionPolicy policy);
+   public GotoBuilder<TState, TEvent, TRecipient, TParent> OnExitPolicy(TransitionPolicy policy);
}

+public sealed class GotoBuilder<TState, TEvent, TRecipient, TParent> : IGoto<TState>
+   where TState : notnull
+   where TEvent : notnull
+{
+   public GotoBuilder<TState, TEvent, TRecipient, TParent> OnEntryPolicy(TransitionPolicy policy);
+   public GotoBuilder<TState, TEvent, TRecipient, TParent> OnExitPolicy(TransitionPolicy policy);
+   public TParent Goto(TState state);
+   public TParent GotoSelf();
+}

+public enum TransitionPolicy
+{
+   Ignore = 0,
+   ParentFirst = 1,
+   ChildFirst = 2,
+   ParentFirstWithCulling = 3,
+   ChildFirstWithCulling = 4,
+   ParentFirstWithCullingInclusive = 5,
+   ChildFirstWithCullingInclusive = 6,
+}
```
- Fix error message when transition is not found in current state.

## 0.3.0
- Add nullable reference analysis.
- Add support for storing a recipient and decoupling parameters from type signature.
- Add proper support for recursive fire calls.
- Add additional properties and methods in the state machine to inspect the state machine.
- Modify the following APIs:
```diff
- public sealed partial class MasterTransitionBuilder<TState, TEvent, TParameter> : TransitionBuilder<TState, TEvent, TParameter> { }

- public sealed partial class SlaveTransitionBuilder<TState, TEvent, TParameter, TParent> : TransitionBuilder<TState, TEvent, TParameter> { }

- public sealed partial class StateBuilder<TState, TEvent, TParameter> { }
+ public sealed partial class StateBuilder<TState, TEvent, TRecipient>
+   where TState : notnull
+   where TEvent : notnull
{ }

- public sealed partial class StateMachineBuilder<TState, TEvent, TParameter>
+ public sealed partial class StateMachineBuilder<TState, TEvent, TRecipient>
+   where TState : notnull
+   where TEvent : notnull
{ }

- public abstract partial class TransitionBuilder<TState, TEvent, TParameter> { }
+ public sealed class TransitionBuilder<TState, TEvent, TRecipient, TParent>
+   where TState : notnull
+   where TEvent : notnull

- public sealed partial class StateMachine<TState, TEvent, TParameter>
+ public sealed partial class StateMachine<TState, TEvent, TRecipient>
+   where TState : notnull
+   where TEvent : notnull
{
-   public TState State { get; }
+	public TState CurrentState { get; }

+   public ReadOnlySlice<TState> CurrentStateHierarchy { get; }
+   public ReadOnlySlice<TEvent> CurrentAcceptedEvents { get; }

-   public static StateMachineBuilder<TState, TEvent, TParameter> Builder()
+   public static StateMachineBuilder<TState, TEvent, TRecipient> CreateFactoryBuilder();

-   public void Start();
-   public void Start(TParameter parameter);

+   public bool GetParentStateOf(TState state, [NotNullWhen(true)] out TState? parentState);
+   public ReadOnlySlice<TState> GetParentHierarchyOf(TState state);
+   public ReadOnlySlice<TEvent> GetAcceptedEventsBy(TState state);
+   public bool IsInState(TState state);

-   public void Fire<TParameter>(TEvent @event, TParameter parameter);
+   public void FireWithParameter<TParameter>(TEvent @event, TParameter parameter);
+   public FireParametersBuilder FireWithParameters(TEvent @event);

+   public void FireImmediately(TEvent @event);
+   public void FireImmediatelyWithParameter<TParameter>(TEvent @event, TParameter parameter);
+   public FireImmediatelyParametersBuilder FireWithParameters(TEvent @event);

-   public void Update<TParameter>(TParameter parameter);
+   public void UpdateWithParameter<TParameter>(TParameter parameter);
+   public UpdateParametersBuilder UpdateWithParameters();

+ public readonly struct CreateParametersBuilder
+ {
+     public CreateParametersBuilder With<TParameter>(TParameter parameter);
+     public StateMachine<TState, TEvent, TRecipient> Done();
+ }

+ public readonly struct FireParametersBuilder
+ {
+     public FireParametersBuilder With<TParameter>(TParameter parameter);
+     public void Done();
+ 

+ public readonly struct FireImmediatelyParametersBuilder
+ {
+     public FireImmediatelyParametersBuilder With<TParameter>(TParameter parameter);
+     public void Done();
+ }

+ public readonly struct UpdateParametersBuilder
+ {
+     public UpdateParametersBuilder With<TParameter>(TParameter parameter);
+     public void Done();
+ }
}

+ public sealed class StateMachineFactory<TState, TEvent, TRecipient>
+   where TState : notnull
+   where TEvent : notnull
+ {
+    public StateMachine<TState, TEvent, TRecipient> Create(TRecipient recipient);
+    public StateMachine<TState, TEvent, TRecipient> CreateWithParameter<TParameter>(TRecipient recipient, TParameter parameter);
+    public StateMachine<TState, TEvent, TRecipient>.CreateParametersBuilder CreateWithParameters(TRecipient recipient);
+ }
```

## 0.2.1
- Modify the following APIs:
```diff
public sealed partial class MasterTransitionBuilder<TState, TEvent, TParameter> : TransitionBuilder<TState, TEvent, TParameter>
-   where TState : IComparable
-   where TEvent : IComparable
{ }

public sealed partial class SlaveTransitionBuilder<TState, TEvent, TParameter, TParent> : TransitionBuilder<TState, TEvent, TParameter>
-   where TState : IComparable
-   where TEvent : IComparable
{ }

public sealed partial class StateBuilder<TState, TEvent, TParameter>
-   where TState : IComparable
-   where TEvent : IComparable
{ }

public sealed partial class StateMachineBuilder<TState, TEvent, TParameter>
-   where TState : IComparable
-   where TEvent : IComparable
{ }

public abstract partial class TransitionBuilder<TState, TEvent, TParameter>
-   where TState : IComparable
-   where TEvent : IComparable
{ }

public sealed partial class StateMachine<TState, TEvent, TParameter>
-   where TState : IComparable
-   where TEvent : IComparable
{ }
```

## 0.2.0
- Fix documentation references.
- Support multiple calls to delegate subscription methods instead of throwing `InvalidOperationException` on the second call.
- Improve perfomance by passing internal large structs by reference.
- Modify the following APIs:
```diff
- public partial class MasterTransitionBuilder<TState, TEvent> : TransitionBuilder<TState, TEvent> 
+ public sealed partial class MasterTransitionBuilder<TState, TEvent, TParameter> : TransitionBuilder<TState, TEvent>
{
-   public MasterTransitionBuilder<TState, TEvent, TParameter> Execute(Action<TParameter> action);
+   public MasterTransitionBuilder<TState, TEvent, TParameter> Do(Action<TParameter> action);
-   public MasterTransitionBuilder<TState, TEvent, TParameter> Execute(Action action);
+   public MasterTransitionBuilder<TState, TEvent, TParameter> Do(Action action);
}

- public partial class SlaveTransitionBuilder<TState, TEvent, TParent> : TransitionBuilder<TState, TEvent>
+ public sealed partial class SlaveTransitionBuilder<TState, TEvent, TParameter, TParent> : TransitionBuilder<TState, TEvent>
{
-   public MasterTransitionBuilder<TState, TEvent, TParameter> Execute(Action<TParameter> action);
+   public MasterTransitionBuilder<TState, TEvent, TParameter> Do(Action<TParameter> action);
-   public MasterTransitionBuilder<TState, TEvent, TParameter> Execute(Action action);
+   public MasterTransitionBuilder<TState, TEvent, TParameter> Do(Action action);
}

- public partial class StateBuilder<TState, TEvent>
+ public sealed partial class StateBuilder<TState, TEvent, TParameter>
{
-   public StateBuilder<TState, TEvent, TParameter> ExecuteOnEntry(Action action);
+   public StateBuilder<TState, TEvent, TParameter> OnEntry(Action action);
-   public StateBuilder<TState, TEvent, TParameter> ExecuteOnEntry(Action<TParameter> action);
+   public StateBuilder<TState, TEvent, TParameter> OnEntry(Action<TParameter> action);
-   public StateBuilder<TState, TEvent, TParameter> ExecuteOnExit(Action action);
+   public StateBuilder<TState, TEvent, TParameter> OnExit(Action action);
-   public StateBuilder<TState, TEvent, TParameter> ExecuteOnExit(Action<TParameter> action);
+   public StateBuilder<TState, TEvent, TParameter> OnExit(Action<TParameter> action);
-   public StateBuilder<TState, TEvent, TParameter> ExecuteOnUpdate(Action action);
+   public StateBuilder<TState, TEvent, TParameter> OnUpdate(Action action);
-   public StateBuilder<TState, TEvent, TParameter> ExecuteOnUpdate(Action<TParameter> action);
+   public StateBuilder<TState, TEvent, TParameter> OnUpdate(Action<TParameter> action);
}

- public partial class StateMachine<TState, TEvent> { }
+ public sealed partial class StateMachine<TState, TEvent, TParameter> { }

- public partial class StateMachineBuilder<TState, TEvent> { }
+ public sealed partial class StateMachineBuilder<TState, TEvent, TParameter> { }

- public abstract partial class TransitionBuilder<TState, TEvent> { }
+ public abstract class TransitionBuilder<TState, TEvent, TParameter> { }
```

## 0.1.1
- Fix `.GotoSelf()` requiring an state and producing `StackOverflowException`.
- Fix documentation references.
- Remove visibility of the following APIs:
```diff
public abstract partial class TransitionBuilder<TState, TEvent>
{
-    protected abstract bool HasSubTransitions();
-    protected int GetGoto(Dictionary<TState, int> statesMap);
}

public partial class MasterTransitionBuilder<TState, TEvent> : TransitionBuilder<TState, TEvent>
{
-    protected override bool HasSubTransitions();
}

public partial class SlaveTransitionBuilder<TState, TEvent, TParent> : TransitionBuilder<TState, TEvent>
{
-    protected override bool HasSubTransitions();
}
```

## 0.1.0
Initial Release