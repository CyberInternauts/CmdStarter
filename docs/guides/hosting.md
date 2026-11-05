# Hosting with Generic Host

The `cints.CmdStarter.Hosting` package runs a CmdStarter application on the .NET Generic Host, with the host's services and hosted services.

## The package

Generic Host support is a separate NuGet package, `cints.CmdStarter.Hosting`, which depends on `Microsoft.Extensions.Hosting`. The core package `cints.CmdStarter` does not depend on it. Its namespace is `CyberInternauts.CmdStarter.Hosting`.

```powershell
dotnet add package cints.CmdStarter.Hosting
```

Without the Hosting package, `IServiceCollection.AddCmdStarter(...)` still registers CmdStarter with plain Microsoft DI, with no reference to `Microsoft.Extensions.Hosting`.

## Register the application

`AddCmdStarter` on `IHostApplicationBuilder` registers CmdStarter with the host's services, through `IServiceCollection.AddCmdStarter`, and replaces the host's console lifetime. Its optional argument is an `Action<CmdStarterOptions>`. `CmdStarterOptions` offers `AddCommandsFromAssembly`, `ConfigureStarter`, `AddFilter<TFilter>`, `UseRepl` and `UseTree`.

In the Hosting sample, the application binds a configuration section to an options class, then registers CmdStarter and runs the command:

<!-- snippet: samples/CmdStarter.Samples.Hosting/Program.cs -->
```csharp
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory });
builder.Services.Configure<GreetingOptions>(builder.Configuration.GetSection("Greeting"));
builder.AddCmdStarter(options => options.AddCommandsFromAssembly(typeof(Program).Assembly));
using var host = builder.Build();
return await host.RunCmdStarterAsync(args);
```

The sample sets `ContentRootPath = AppContext.BaseDirectory`, so that `appsettings.json` is found beside the executable. See [ADR 0015](../adr/0015-generic-host-integration.md) and [Configuration](configuration.md).

## Run the host

`host.RunCmdStarterAsync(args)` is an extension of `IHost`, with an optional `CancellationToken`. It starts the host, so that the hosted services run, runs the command, and returns its exit code. It always stops the host afterwards.

For example, a hosted service runs around the command:

<!-- snippet: CmdStarter.Tests/Hosting.cs -->
```csharp
var builder = CreateBuilder();
builder.Services.AddSingleton<LifecycleRecorder>();
builder.Services.AddHostedService<RecordingHostedService>();
using var host = builder.Build();

await host.RunCmdStarterAsync(["probe"]);
```

## Services in commands

Under the host, command classes and method parameters are resolved from the host's container. Configuration, options (`IOptions<T>`) and logging (`ILogger<T>`) are injected like any other service. In the Hosting sample, the `greet` command receives its options and its logger as parameters:

<!-- snippet: samples/CmdStarter.Samples.Hosting/Commands/GreetCommands.cs -->
```csharp
[Command]
public static void Greet(string name, IOptions<GreetingOptions> options, ILogger<GreetCommands> logger)
```

```powershell
dotnet run --project samples/CmdStarter.Samples.Hosting -- greet Ann
```

See [Configuration](configuration.md) for how the values are read.

## A REPL under the host

A REPL runs under the host too. Configure `UseRepl(provider)` in the options. `RunCmdStarterAsync` then runs the loop, and it returns 0 when the loop ends. With no arguments, the loop starts at once. For example, with an input provider that supplies two lines:

<!-- snippet: CmdStarter.Tests/Hosting.cs -->
```csharp
var builder = CreateBuilder(options => options.UseRepl(new ScriptedInputProvider("probe", "probe")));
using var host = builder.Build();

Assert.That(await host.RunCmdStarterAsync([]), Is.Zero);
```

See [REPL](repl.md).

## Console and lifetime

The default console lifetime is replaced by `CmdStarterHostLifetime`, which does nothing, so the output carries no host startup message such as `Application started`. The default lifetime would also take Ctrl+C to stop the whole host instead of the running command ([ADR 0015](../adr/0015-generic-host-integration.md)).

## See also

- Examples: [Hosting](../../samples/CmdStarter.Samples.Hosting).
- Neighbouring pages: [Configuration](configuration.md), [REPL](repl.md), [Dependency injection](../concepts/dependency-injection.md), [Testing](testing.md).
- ADR: [0015, Generic Host integration](../adr/0015-generic-host-integration.md).
