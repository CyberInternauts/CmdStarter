# Testing

This guide shows how to test a CmdStarter application in memory, with a fake service, and how to check the exit code of a command.

## Keep the composition in one method

Put the composition of the application in a method that returns the `CmdStarterBuilder`, and use that method both in `Main` and in your tests. In the Testing sample, `TransferApp.CreateBuilder()` creates the builder, adds the commands of the assembly and registers `IAccounts`. `Main` builds it and runs it.

<!-- snippet: samples/CmdStarter.Samples.Testing/TransferApp.cs -->
```csharp
public static class TransferApp
{
    public static CmdStarterBuilder CreateBuilder()
    {
        var builder = new CmdStarterBuilder().AddCommandsFromAssembly(typeof(TransferApp).Assembly);
        builder.Services.AddSingleton<IAccounts, InMemoryAccounts>();
        return builder;
    }
}
```

<!-- snippet: samples/CmdStarter.Samples.Testing/Program.cs -->
```csharp
await using var app = TransferApp.CreateBuilder().Build();
return await app.RunAsync(args);
```

## Replace a service with a fake

Before you build, replace the service with a fake: `builder.Services.Replace(ServiceDescriptor.Singleton(typeof(IAccounts), fake))`, then call `builder.Build()`. `Replace` is an extension method from `Microsoft.Extensions.DependencyInjection.Extensions`. A builder can be built only once, so each test creates its own: a second `Build` throws `InvalidOperationException`.

<!-- snippet: samples/CmdStarter.Samples.Testing.Tests/TransferTests.cs -->
```csharp
using Microsoft.Extensions.DependencyInjection.Extensions;

[Test]
public async Task TransfersThroughTheAccountsService()
{
    var builder = TransferApp.CreateBuilder();
    var fake = new FakeAccounts(true);
    builder.Services.Replace(ServiceDescriptor.Singleton(typeof(IAccounts), fake));
    await using var app = builder.Build();
    var result = await app.RunAsync(["transfer", "alice", "bob", "--amount", "10"]);
    Assert.That(result, Is.Zero);
    Assert.That(fake.Calls, Is.EqualTo(new[] { ("alice", "bob", 10) }));
}
```

## Run in memory and check the exit code

Run the application in memory with `CmdStarterApp.RunAsync(string[])`, which returns the exit code as a `Task<int>`. Dispose the application with `await using`, then assert the exit code and what the fake recorded.

## The handler decides the exit code

The handler decides the exit codes, so a test can check the code rather than the output: `0` for a transfer that is done, `1` when the service refuses it, and `2` for an amount that is not positive. The amount option defaults to `0`.

<!-- snippet: samples/CmdStarter.Samples.Testing/Commands/TransferCommands.cs -->
```csharp
[Command]
public static int Transfer(string from, string to, IAccounts accounts, [Option] int amount = 0)
{
    if (amount <= 0)
    {
        Console.Error.WriteLine("The amount must be positive.");
        return 2;
    }

    if (!accounts.Transfer(from, to, amount))
    {
        Console.Error.WriteLine("Transfer refused.");
        return 1;
    }

    Console.Out.WriteLine($"Transferred {amount} from {from} to {to}.");
    return 0;
}
```

## A fake that records its calls

The fake is a plain class. It records its calls in `Calls` and returns the answer it was given, `accept`.

<!-- snippet: samples/CmdStarter.Samples.Testing.Tests/FakeAccounts.cs -->
```csharp
public sealed class FakeAccounts(bool accept) : IAccounts
{
    public List<(string From, string To, int Amount)> Calls { get; } = [];

    public bool Transfer(string from, string to, int amount)
    {
        Calls.Add((from, to, amount));
        return accept;
    }
}
```

## Run the sample from the command line

The sample also runs from the command line. The three runs below exit with 0 for a transfer of 10, with 2 for `--amount 0`, and with 1 for an unknown account `carol`.

```powershell
dotnet run --project samples/CmdStarter.Samples.Testing -- transfer alice bob --amount 10
dotnet run --project samples/CmdStarter.Samples.Testing -- transfer alice bob --amount 0
dotnet run --project samples/CmdStarter.Samples.Testing -- transfer alice carol --amount 10
```

The first prints `Transferred 10 from alice to bob.`. The second reports `The amount must be positive.` as an error, and the third reports `Transfer refused.` as an error.

The sample's own tests are an ordinary NUnit project that references the sample app. They run with the rest of the solution, and you can select them with a filter:

```powershell
dotnet test CmdStarter.sln --filter "FullyQualifiedName~CmdStarter.Samples.Testing.Tests"
```

## Capture a command's output

To capture a command's output in memory, give the handler a `CommandConsole` parameter. In the class model it is a handler parameter, and in the method model it is a method parameter. The handler then writes to the console you pass to `Starter.Start`, not to `System.Console`. The sample's handlers write to `System.Console` and do not use it.

<!-- snippet: CmdStarter.Tests.Commands/Consoles/ConsoleCommands.cs -->
```csharp
public sealed class Write : StarterCommand
{
    public override Delegate HandlingMethod => (Func<CommandConsole, int>)(console =>
    {
        console.Out.WriteLine("class out");
        console.Error.WriteLine("class err");
        return 0;
    });
}
```

Pass `new CommandConsole(input, output, error)` as the second argument of `Starter.Start`, then read what the handler wrote from your writers.

<!-- snippet: CmdStarter.Tests/CommandConsoles.cs -->
```csharp
[Test]
public async Task ExplicitConsoleReceivesHandlerOutput()
{
    var starter = CreateStarter();

    Assert.That(await starter.Start("write", CreateConsole()), Is.EqualTo(0));
    Assert.That(output.ToString(), Does.Contain("class out"));
    Assert.That(error.ToString(), Does.Contain("class err"));
}

private CommandConsole CreateConsole() => new(new StringReader(string.Empty), output, error);
```

## A default console

A `Starter` can have a default console, `Starter.DefaultConsole`. CmdStarter uses it for each `Start` that receives none.

## Capture an application's output

`CmdStarterApp.RunAsync` has no console parameter. To capture an application's output in memory, pass a console through its `Starter`: `app.Starter.Start("echo hello", console)` writes `hello` to that console. You can also set `app.Starter.DefaultConsole` and then call `app.RunAsync`.

<!-- snippet: CmdStarter.Tests/CommandConsoles.cs -->
```csharp
await using var app = new CmdStarterBuilder()
    .AddCommandsFromAssembly(typeof(Write).Assembly)
    .ConfigureStarter(starter => starter.Namespaces = ImmutableList.Create(typeof(Write).Namespace!))
    .Build();

// RunAsync has no console parameter: the console goes through the app's Starter.
Assert.That(await app.Starter.Start("echo hello", CreateConsole()), Is.EqualTo(0));
Assert.That(output.ToString(), Does.Contain("hello"));

app.Starter.DefaultConsole = CreateConsole();
Assert.That(await app.RunAsync("echo again"), Is.EqualTo(0));
Assert.That(output.ToString(), Does.Contain("again"));
```

## Without the builder

Without `CmdStarterBuilder`, create a `Starter`, restrict discovery with `Namespaces` or `Classes`, and call `Start("…")`. The result is the exit code. CmdStarter's own test suite works this way:

<!-- snippet: CmdStarter.Tests/ValidationAttributes.cs -->
```csharp
private static Starter CreateStarter()
{
    var starter = new Starter();
    starter.Namespaces = starter.Namespaces.Clear().Add(typeof(LevelCommand).Namespace!);
    return starter;
}
```

## See also

- Examples: [Testing](../../samples/CmdStarter.Samples.Testing), [Testing.Tests](../../samples/CmdStarter.Samples.Testing.Tests).
- Neighbouring pages: [Dependency injection](../concepts/dependency-injection.md), [Command console](command-console.md), [Errors and exit codes](errors-and-exit-codes.md), [Configuration](configuration.md).
- ADR: [0006, dependency injection](../adr/0006-dependency-injection.md), [0021, command console](../adr/0021-command-console.md).
