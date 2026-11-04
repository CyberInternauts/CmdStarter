# Commands

CmdStarter turns C# code into a command-line tree. A command is declared in one of two models: the **class model**, where a class is a command, and the **method model**, where a method is a command. Both models can be used in the same application. This page covers each model, how a handler sets the exit code, and how names are derived. The rules that find and place commands are in [Discovery](discovery.md). The rules that turn properties and parameters into options and arguments are in [Binding](binding.md).

## The class model

A class command is a non-abstract class that implements `IStarterCommand` and has a namespace. Most command classes derive from the abstract base class `StarterCommand`:

<!-- snippet: samples/CmdStarter.Samples.HelloWorld/Commands/Hello.cs -->
```csharp
public sealed class Hello : StarterCommand
{
    public string To { get; set; } = "World";

    public override Delegate HandlingMethod => (Action)(() => Console.Out.WriteLine($"Hello, {To}!"));
}
```

The entry point passes the arguments to a `Starter`:

<!-- snippet: samples/CmdStarter.Samples.HelloWorld/Program.cs -->
```csharp
public static Task<int> Main(string[] args) => new Starter().Start(args);
```

`Starter.Start` returns a `Task<int>`, so `Main` can return it directly.

A class does not have to derive from `StarterCommand`. A class that implements `IStarterCommand` directly must provide the `HandlingMethod` property and a settable `GlobalOptionsManager` property:

<!-- snippet: CmdStarter.Demo.SimpleInjector/Commands/CmdService.cs -->
```csharp
internal class CmdService : IStarterCommand
{
    // ...
    public int MyInt { get; set; } = 111;
    // ...
    public GlobalOptionsManager? GlobalOptionsManager { get; set; }

    public Delegate HandlingMethod => () =>
    // ...
```

### The handler

`HandlingMethod` is a `Delegate` property. The default implementation does nothing (`IStarterCommand.EmptyExecution`). A command overrides it to return its handler, written as a lambda cast to a delegate type, or as a method group. The handler's parameters become the command's positional arguments, in order (see [Binding](binding.md)):

<!-- snippet: samples/CmdStarter.Samples.TwoCommands/Commands/Add.cs -->
```csharp
public sealed class Add : StarterCommand
{
    public override Delegate HandlingMethod => (Action<int, int>)((left, right) => Console.Out.WriteLine(left + right));
}
```

Common forms are `(Action)(() => ...)`, `(Action<int, int>)((left, right) => ...)`, `(Func<int>)(() => ...)`, and a method group such as `Execute`.

### Return values

The value that the handler returns becomes the exit code of the process:

| The handler returns | Exit code |
|---|---|
| `void` | `0` |
| `int` | that value |
| `Task<int>` | the value the task produces, once awaited |
| `ValueTask<int>` | the value the task produces, once awaited |
| `Task` or `ValueTask` | `0`, once awaited |

### Only leaf commands run a handler

Only the leaf commands of the tree run a handler. A command that has subcommands is the path to them. In the [NestedCommands](../../samples/CmdStarter.Samples.NestedCommands) sample, `remote add origin ...` runs the leaf command `add`.

## The method model

A method marked with `[Command]` is a command. The method model is built with these shapes:

- a class marked `[Command("group")]` becomes a group under the root, and its `[Command]` methods become its subcommands;
- a class without `[Command]` on the class puts its `[Command]` methods at the top level.

<!-- snippet: samples/CmdStarter.Samples.DevOpsTool/Commands/ServiceCommands.cs -->
```csharp
[Command("service")]
public sealed class ServiceCommands(IDeploymentClient client)
{
    // ...
    [Command]
    public async Task<int> DeployAsync(string name, DeployOptions options, [Option] string version = "latest")
    // ...
    [Command]
    public int Rollback(string name, DeployOptions options)
```

A static method is a command too, with no class instance to create:

<!-- snippet: samples/CmdStarter.Samples.DependencyInjection/Commands/WelcomeCommands.cs -->
```csharp
public sealed class WelcomeCommands
{
    [Command]
    public static void Welcome(string name, IGreetingService service)
```

The subcommands of a group appear in the order in which their methods are declared in the class.

### Names of method commands

A method command's name is the explicit name given in `[Command("name")]`. Without one, it is the method name in kebab-case, with a trailing `Async` removed: `DeleteAsync` becomes `delete`, and `WaitAsync` becomes `wait`. An explicit name is never shortened. The following methods show the three cases, and the return types of the table above:

<!-- snippet: CmdStarter.Tests.Commands/Methods/ToolCommands.cs -->
```csharp
[Command("ping")] public static int Ping() => 7;
[Command, Alias("st")] public static int Status(MainGlobalOptions globals) => globals.IntGlobalOption;
[Command] public static async ValueTask<int> WaitAsync() { await Task.Yield(); return 3; }
```

### Paths and implicit groups

A `[Command]` name can be a path of words separated by spaces. Each word but the last must match an existing subcommand (a class command, a group, or a leaf command). When no subcommand matches, CmdStarter creates an **implicit group** for that word: a group with no handler of its own. An explicit group declared later on the same path adopts the implicit group, so the tree has one `zone` command, not two. Two explicit groups on the same path throw `InvalidAttributeException`.

<!-- snippet: CmdStarter.Tests.Commands/MethodGroups/MethodGroupCommands.cs -->
```csharp
[Command("admin users")]
public sealed class AdminUserCommands
// ...
public sealed class ReportCommands
{
    [Command("report daily")]
    public static int Daily() => 14;
}

[Command("audit log")]
public sealed class AuditLogCommands
```

### Invalid method declarations

The following declarations throw `InvalidAttributeException` when the command tree is built:

- a `[Command]` method that is not public, that is generic, or that has `ref`, `out` or `in` parameters;
- a class that implements `IStarterCommand` and also has `[Command]` methods;
- a `[Command("group")]` class that has no `[Command]` method;
- an abstract class that is not static and that has instance commands;
- two commands with the same name under the same parent.

### Using both models

The two models can be used in the same application. Class commands and method commands are added to the same root. When there is at least one method command, a lonely class command is not rooted (see [Lonely command](#the-lonely-command)).

## Naming class commands

The name of a class command is its type name in kebab-case. `NameFor` becomes `name-for`, `FirstByInterface` becomes `first-by-interface`, and `Word` becomes `word`. A command that only implements `IStarterCommand` gets the kebab-case of its type name.

A `StarterCommand` can give an explicit name through its base constructor:

<!-- snippet: CmdStarter.Tests.Commands/Loader/Naming/NameOverride.cs -->
```csharp
public class NameOverride : StarterCommand
{
    public NameOverride() : base("NameOverridden") { }
}
```

`base("NameOverridden")` gives the name `name-overridden`. A name that contains `-` is kept in lowercase as it is, so `base("name-to-kebab")` gives `name-to-kebab`. Any other name is converted to kebab-case.

Option names follow the same kebab-case rule (`MyInt` becomes `--my-int`). Argument names are the handler's parameter names as they are written. See [Binding](binding.md).

## The lonely command

When the application has exactly one class command, no method command, and that command is not placed in a group, the command is **lonely**. With `Starter.IsRootingLonelyCommand` set to its default `true`, the lonely command's options, arguments and handler are put on the root command, so it runs without typing its name:

```powershell
dotnet run --project samples/CmdStarter.Samples.HelloWorld -- --to Bob
```

With `Starter.IsRootingLonelyCommand` set to `false`, the command name is required.

## See also

- Examples: [HelloWorld](../../samples/CmdStarter.Samples.HelloWorld), [TwoCommands](../../samples/CmdStarter.Samples.TwoCommands), [DevOpsTool](../../samples/CmdStarter.Samples.DevOpsTool), [DependencyInjection](../../samples/CmdStarter.Samples.DependencyInjection).
- Neighbouring pages: [Discovery](discovery.md), [Attributes](attributes.md), [Binding](binding.md), [Dependency injection](dependency-injection.md), [Nested commands](../guides/nested-commands.md).
- ADRs: [0004, class-based command model](../adr/0004-class-based-command-model.md), [0009, method-based command model](../adr/0009-method-based-command-model.md).
