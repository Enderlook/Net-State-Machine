![.NET Core](https://github.com/Enderlook/Net-State-Machine/workflows/.NET%20Core/badge.svg?branch=master)

# .NET State Machine

An state machine builder library for .NET.

The following example show all the functions of the state machine (except overloads which are described bellow):

```cs
public class Character
{
	private Random rnd = new Random();
	private StateMachine<States, Events> stateMachine;
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
		stateMachine = StateMachine<States, Events>.Builder()
			.SetInitialState(States.Sleep)
			.In(States.Sleep)
				// Executed every time we enter to this state
				.ExecuteOnEntry(() => Console.WriteLine("Going to bed."))        
				// Executed every time we exit from this state
				.ExecuteOnExit(() => Console.WriteLine("Getting up."))
				// Executed every time StateMachine.Update() is executed and is in this state
				.ExecuteOnUpdate(OnUpdateSleep)
				.On(Events.HasFullHealth)
					// Executed every time this event is raised in this state
					.Execute(() => Console.WriteLine("Pick toys."))
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
				.ExecuteOnUpdate(OnUpdatePlay)
				.On(Events.IsHungry)
					.If(IsWounded)
						.Goto(States.Gather)
					.Goto(States.Hunt)
			.In(States.Gather)
				.ExecuteOnUpdate(OnUpdateGather)
				.On(Events.IsNoLongerHungry)
					.If(IsWounded)
						.Goto(States.Sleep)
					.Goto(States.Play)
				.On(Events.HasFullHealth)
					.Goto(States.Hunt)
			.In(States.Hunt)
				.ExecuteOnEntry(() => Console.WriteLine("Take bow."))
				.ExecuteOnExit(() => Console.WriteLine("Drop bow."))
				.ExecuteOnUpdate(OnUpdateHunt)
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

`Fire(TEvent)` and `Fire(TEvent, Object)`. The method accept an optional parameter `Object` which is send to every delegate. If `Object` is missing, `null` is used.

`ExecuteOnEntry(Action)`, `ExecuteOnExit(Action)`, `ExecuteOnUpdate(Action)` and `Execute(Action)` has an additional overload: `ExecuteOnEntry(Action<Object>)`, `ExecuteOnExit(Action<Object>)`, `ExecuteOnUpdate(Action<Object>)` and `Execute(Action<Object>)`, where `object` parameter is the value passed on `.Fire(TEvent, Object)`.

The parameter `Object` is optional, and can be ignored.
For example: 
 - If you use `Fire(TEvent)` and `Execute(Action<Object>)`, `Object` is replaced by `null`.
 - If you use `Fire(TEvent, Object)` but `Execute(Action)`, `Object` is ignored.

`If(Func<bool>)` also have `If(Func<Object, bool)`.

`Start()` also have `Start(Object)`, where `Object` is used in `ExecuteOnEntry(Action<Object>)` of the initial state.
