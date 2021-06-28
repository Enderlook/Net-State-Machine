![.NET Core](https://github.com/Enderlook/Net-State-Machine/workflows/.NET%20Core/badge.svg?branch=master)

# .NET State Machine

An state machine builder library for .NET.

The following example show all the functions of the state machine (except overloads which are described bellow):

```cs
public class Character
{
	private Random rnd = new Random();
	private StateMachine<States, Events, object> stateMachine;
	private int health = 100;
	private int food = 100;

	enum States
	{
		Sleep,
		Hunt,
		Gather,
		Play,
	}

	enum Events
	{
		HasFullHealth,
		LowHealth,
		IsHungry,
		IsNoLongerHungry,
	}

	public static async Task Main()
	{
		Character character = new Character();
		while (true)
		{
			Console.Clear();
			character.stateMachine.Update();
			Console.WriteLine($"State: {character.stateMachine.State}.");
			Console.WriteLine($"Health: {character.health}.");
			Console.WriteLine($"Food: {character.food}.");
			await Task.Delay(10).ConfigureAwait(false);
		}
	}

	public Character()
	{
		stateMachine = StateMachine<States, Events, object>.Builder()
			.SetInitialState(States.Sleep)
			.In(States.Sleep)
				// Executed every time we enter to this state
				.OnEntry(() => Console.WriteLine("Going to bed."))        
				// Executed every time we exit from this state
				.OnExit(() => Console.WriteLine("Getting up."))
				// Executed every time StateMachine.Update() is executed and is in this state
				.OnUpdate(OnUpdateSleep)
				.On(Events.HasFullHealth)
					// Executed every time this event is raised in this state
					.Do(() => Console.WriteLine("Pick toys."))
					.Goto(States.Play)
				.On(Events.IsHungry)
					.If(IsVeryWounded)
						// We stay in our current state without executing OnEntry nor OnExit delegates
						.StaySelf()
						// If we wanted to execute those delegates we can use
						//.Goto(States.Sleep)
						// Or
						//.GotoSelf()
					// Else
					.If(IsWounded)
						.Goto(States.Gather)
					// Else
					.Goto(States.Hunt)
				// Ignore this event in this transition.
				// (If we don't add this and we accidentally raise this event an exception is thrown).
				.Ignore(Events.LowHealth)
			.In(States.Play)
				.OnUpdate(OnUpdatePlay)
				.On(Events.IsHungry)
					.If(IsWounded)
						.Goto(States.Gather)
					.Goto(States.Hunt)
			.In(States.Gather)
				.OnUpdate(OnUpdateGather)
				.On(Events.IsNoLongerHungry)
					.If(IsWounded)
						.Goto(States.Sleep)
					.Goto(States.Play)
				.On(Events.HasFullHealth)
					.Goto(States.Hunt)
			.In(States.Hunt)
				.OnEntry(() => Console.WriteLine("Take bow."))
				.OnExit(() => Console.WriteLine("Drop bow."))
				.OnUpdate(OnUpdateHunt)
				.On(Events.IsNoLongerHungry)
					.Goto(States.Sleep)
				.On(Events.LowHealth)
					.Goto(States.Sleep)
			.Build();
 
		stateMachine.Start();
	}

	private void OnUpdateHunt()
	{
		food += rnd.Next(8);
		if (food >= 100)
		{
			food = 100;
			stateMachine.Fire(Events.IsNoLongerHungry);
		}

		health -= rnd.Next(6);
		if (health <= 20)
			stateMachine.Fire(Events.LowHealth);
	}

	private void OnUpdateGather()
	{
		food += rnd.Next(3);
		if (food >= 100)
		{
			food = 100;
			stateMachine.Fire(Events.IsNoLongerHungry);
		}

		if (rnd.Next(2) % 1 == 0)
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

	private bool IsVeryWounded() => health <= 50;

	private bool IsWounded() => health <= 75;

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