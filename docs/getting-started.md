# Getting started

This page installs CmdStarter and runs three examples: HelloWorld, TwoCommands and DependencyInjection.

## Requirements

CmdStarter targets .NET 10 only. An application that uses it must target `net10.0`, and must be built with the .NET 10 SDK:

```xml
<TargetFramework>net10.0</TargetFramework>
```

## Install the package

```powershell
dotnet add package cints.CmdStarter --prerelease
```

The package is a pre-release, so `--prerelease` is needed; in a NuGet interface, tick the option that includes pre-releases.

## HelloWorld

The program returns the exit code of `Starter.Start`:

<!-- snippet: samples/CmdStarter.Samples.HelloWorld/Program.cs -->
```csharp
public static Task<int> Main(string[] args) => new Starter().Start(args);
```

The command is one class that derives from `StarterCommand`:

<!-- snippet: samples/CmdStarter.Samples.HelloWorld/Commands/Hello.cs -->
```csharp
public sealed class Hello : StarterCommand
{
    public string To { get; set; } = "World";

    public override Delegate HandlingMethod => (Action)(() => Console.Out.WriteLine($"Hello, {To}!"));
}
```

The public read/write property `To` becomes the option `--to`. The handler is `HandlingMethod`. Since it is the only command, it is rooted: it runs without a command name.

```powershell
dotnet run --project samples/CmdStarter.Samples.HelloWorld -- --to Bob
```

This prints `Hello, Bob!`.

## TwoCommands

With more than one command, each is typed by its name, which comes from the class name in kebab-case: the classes `Add` and `Subtract` become the commands `add` and `subtract`. The handler's parameters become positional arguments, converted to the parameter types.

<!-- snippet: samples/CmdStarter.Samples.TwoCommands/Commands/Add.cs -->
```csharp
public sealed class Add : StarterCommand
{
    public override Delegate HandlingMethod => (Action<int, int>)((left, right) => Console.Out.WriteLine(left + right));
}
```

<!-- snippet: samples/CmdStarter.Samples.TwoCommands/Commands/Subtract.cs -->
```csharp
public sealed class Subtract : StarterCommand
{
    public override Delegate HandlingMethod => (Action<int, int>)((left, right) => Console.Out.WriteLine(left - right));
}
```

```powershell
dotnet run --project samples/CmdStarter.Samples.TwoCommands -- add 2 3
dotnet run --project samples/CmdStarter.Samples.TwoCommands -- subtract 5 3
```

The first prints `5`, the second `2`.

## DependencyInjection

This program uses `CmdStarterBuilder` and `AddCommandsFromAssembly`. It registers a service with `builder.Services`, builds the application, and runs it with `RunAsync(args)`:

<!-- snippet: samples/CmdStarter.Samples.DependencyInjection/Program.cs -->
```csharp
var builder = new CmdStarterBuilder().AddCommandsFromAssembly(typeof(Program).Assembly);
builder.Services.AddSingleton<IGreetingService, GreetingService>();
await using var app = builder.Build();
return await app.RunAsync(args);
```

The class command takes the service in its constructor:

<!-- snippet: samples/CmdStarter.Samples.DependencyInjection/Commands/Greet.cs -->
```csharp
public sealed class Greet : StarterCommand
{
    private readonly IGreetingService service;

    public Greet(IGreetingService service) => this.service = service;

    public string To { get; set; } = "World";

    public override Delegate HandlingMethod => (Action)(() => Console.Out.WriteLine(service.Greet(To)));
}
```

The `[Command]` method takes the same service as a parameter. The parameter needs no attribute, because the service is registered:

<!-- snippet: samples/CmdStarter.Samples.DependencyInjection/Commands/WelcomeCommands.cs -->
```csharp
public sealed class WelcomeCommands
{
    [Command]
    public static void Welcome(string name, IGreetingService service)
    {
        Console.Out.WriteLine($"Welcome! {service.Greet(name)}");
    }
}
```

```powershell
dotnet run --project samples/CmdStarter.Samples.DependencyInjection -- greet --to Ann
dotnet run --project samples/CmdStarter.Samples.DependencyInjection -- welcome Ann
```

The first prints `Hello, Ann!`, the second `Welcome! Hello, Ann!`.

## Where to go next

- [Commands](concepts/commands.md): the two command models.
- [Dependency injection](concepts/dependency-injection.md).
- [Nested commands](guides/nested-commands.md) and [Testing](guides/testing.md).
- [Samples](../samples/README.md): all the runnable examples.

See also the [index](index.md).
