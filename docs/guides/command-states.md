# Command states

A command can be available in only some states of your application; this guide shows how CmdStarter hides and refuses the others.

## The state belongs to your application

CmdStarter defines no state. Your application supplies its own values, such as an enum, and an `ICommandStateProvider` whose `CurrentState` property returns the current value. In the CommandStates sample, the enum has two values:

<!-- snippet: samples/CmdStarter.Samples.CommandStates/SessionState.cs -->
```csharp
public enum SessionState
{
    LoggedOut,
    LoggedIn
}
```

The provider is the session object, which keeps the state and the user:

<!-- snippet: samples/CmdStarter.Samples.CommandStates/Services/Session.cs -->
```csharp
public sealed class Session : ICommandStateProvider
{
    public SessionState State { get; set; } = SessionState.LoggedOut;
    public string? User { get; set; }
    public object? CurrentState => State;
}
```

## Mark a command with AvailableInState

`[AvailableInState(...)]` goes on a class command, on a group class of the method model, or on a method. A command without it is always available. In the sample, `login` is available when logged out, and `whoami` when logged in:

<!-- snippet: samples/CmdStarter.Samples.CommandStates/Commands/SessionCommands.cs -->
```csharp
[Command]
[AvailableInState(SessionState.LoggedOut)]
public static void Login(string user, Session session)
// ...
[Command]
[AvailableInState(SessionState.LoggedIn)]
public static void Whoami(Session session)
```

On a group, the attribute applies to every command under that group. In this example, the `vault secrets` group is available only in the `Member` state:

<!-- snippet: CmdStarter.Tests.Commands/States/Commands.cs -->
```csharp
[Command("vault secrets")]
[AvailableInState(TestState.Member)]
public sealed class VaultSecretsCommands
```

The attribute can list several states: the command is then available in any of them, and refused, with code 1, in every other state.

A command is available when its current state equals one of the attribute's values, and when all its ancestors are available. An unavailable command makes its whole subtree unavailable.

An implicit group, a path segment that has no class of its own, stays visible and passes its parent's availability to its children. In the example above, `vault` stays visible while the `vault secrets` group is hidden. See [Nested commands](nested-commands.md).

A command that the application adds to `RootCommand` itself, outside CmdStarter, is left alone.

A lonely class command that is rooted on the root command stays hidden, since its symbols are on the root, but it is refused when its state does not allow it.

## Where the provider comes from

The provider comes from `Starter.StateProvider`. If that is not set, CmdStarter resolves `ICommandStateProvider` from `Starter.ServiceProvider`. With dependency injection, register the provider as a singleton, so that every command scope sees the same state changes. The sample registers `Session` as a singleton, and registers the interface as an alias of it:

<!-- snippet: samples/CmdStarter.Samples.CommandStates/Program.cs -->
```csharp
var builder = new CmdStarterBuilder().AddCommandsFromAssembly(typeof(Program).Assembly);
builder.Services.AddSingleton<Session>();
builder.Services.AddSingleton<ICommandStateProvider>(services => services.GetRequiredService<Session>());
builder.UseRepl(new ConsoleReplProvider());
```

When at least one command has `[AvailableInState]` and no provider is found, building the tree throws `InvalidAttributeException`.

## Each start follows the state

CmdStarter reads the state again at every `Start`, before it parses the line. The tree is not rebuilt. In a REPL, every line follows the current state.

## Hidden and refused

An unavailable command is hidden from the help, and it is refused if it is typed anyway: CmdStarter writes `Command '<path>' is not available in the current state.` to the error output and returns 1. For example, `profile`, a command for members only, is refused in the anonymous state:

<!-- snippet: CmdStarter.Tests/States.cs -->
```csharp
var starter = CreateStarter(new TestStateProvider { CurrentState = TestState.Anonymous });

Assert.That(await starter.Start("profile"), Is.EqualTo(1));
```

Completion in a REPL follows the state too: unavailable commands are not proposed.

Run the sample, then type `login ann`, `whoami` and `logout`.

```powershell
dotnet run --project samples/CmdStarter.Samples.CommandStates
```

If you type `whoami`, `login ann`, `whoami`, `login bob`, `logout` and `exit`, the first `whoami` (before the login) and `login bob` (after it) are refused.

> **Note:** Hiding a command is not a security measure. A state decides what the interface offers; it does not protect a sensitive operation. The handler or the service must check the user's rights itself. A command filter can also refuse an execution (see [Filters](filters.md)).

## See also

- Examples: [CommandStates](../../samples/CmdStarter.Samples.CommandStates).
- Neighbouring pages: [REPL](repl.md), [Filters](filters.md), [Errors and exit codes](errors-and-exit-codes.md), [Attributes](../concepts/attributes.md), [Dependency injection](../concepts/dependency-injection.md).
- ADR: [0012, command states](../adr/0012-command-states.md).
