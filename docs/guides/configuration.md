# Configuration

This guide shows how a CmdStarter application reads its settings from environment variables and from `appsettings.json`, and how the Configuration and Hosting samples combine an option with them.

## Register `IConfiguration`

An application can register its own `IConfiguration` as a service. Command classes and method parameters then receive it like any other registered service. The Configuration sample builds the configuration from environment variables with the prefix `SAMPLE_`, and registers it as a singleton.

<!-- snippet: samples/CmdStarter.Samples.Configuration/Program.cs -->
```csharp
var configuration = new ConfigurationBuilder().AddEnvironmentVariables(prefix: "SAMPLE_").Build();
var builder = new CmdStarterBuilder().AddCommandsFromAssembly(typeof(Program).Assembly);
builder.Services.AddSingleton<IConfiguration>(configuration);
await using var app = builder.Build();
return await app.RunAsync(args);
```

## Environment variables and their names

The prefix is removed from the name. With `AddEnvironmentVariables(prefix: "SAMPLE_")`, the variable `SAMPLE_REGION` is read under the key `Region`.

```powershell
$env:SAMPLE_REGION = "eu-west"; dotnet run --project samples/CmdStarter.Samples.Configuration -- show-region
```

With the variable set as above, the command prints `Region: eu-west`.

## Option, configuration and default

The option is a method parameter with `[Option]` and a `null` default. When you leave it out, `region` is `null`, and the handler falls back on the configuration.

The handler sets the order: `region ?? configuration["Region"] ?? "local"`. An option wins over the environment, and the environment wins over the default `local`. The application writes this order, CmdStarter does not provide it.

<!-- snippet: samples/CmdStarter.Samples.Configuration/Commands/RegionCommands.cs -->
```csharp
[Command]
public static void ShowRegion(IConfiguration configuration, [Option] string? region = null)
{
    Console.Out.WriteLine($"Region: {region ?? configuration["Region"] ?? "local"}");
}
```

With `SAMPLE_REGION` set to `eu-west`, `--region us-east` prints `Region: us-east`. With no variable and no option, `show-region` prints `Region: local`.

```powershell
dotnet run --project samples/CmdStarter.Samples.Configuration -- show-region --region us-east
```

The sample's project references the `Microsoft.Extensions.Configuration.EnvironmentVariables` package.

## Generic Host

The `cints.CmdStarter.Hosting` package adds `AddCmdStarter` to the host builder and `RunCmdStarterAsync` to the host. With Generic Host, `appsettings.json`, environment variables, options and logging come from the host. The Hosting sample binds the `Greeting` section of `appsettings.json` to `GreetingOptions`:

<!-- snippet: samples/CmdStarter.Samples.Hosting/Program.cs -->
```csharp
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory });
builder.Services.Configure<GreetingOptions>(builder.Configuration.GetSection("Greeting"));
builder.AddCmdStarter(options => options.AddCommandsFromAssembly(typeof(Program).Assembly));
using var host = builder.Build();
return await host.RunCmdStarterAsync(args);
```

<!-- snippet: samples/CmdStarter.Samples.Hosting/GreetingOptions.cs -->
```csharp
public sealed class GreetingOptions
{
    public string Prefix { get; set; } = "Hi";
}
```

The file `appsettings.json` of the sample contains:

```json
{
  "Greeting": {
    "Prefix": "Hello"
  }
}
```

The project copies that file to the output folder:

```xml
<None Update="appsettings.json" CopyToOutputDirectory="PreserveNewest" />
```

The command receives the options and the logger as method parameters. The logger is injected, and the sample logs at debug level.

<!-- snippet: samples/CmdStarter.Samples.Hosting/Commands/GreetCommands.cs -->
```csharp
[Command]
public static void Greet(string name, IOptions<GreetingOptions> options, ILogger<GreetCommands> logger)
{
    Console.Out.WriteLine($"{options.Value.Prefix}, {name}!");
    logger.LogDebug("Greeted {Name}", name);
}
```

```powershell
dotnet run --project samples/CmdStarter.Samples.Hosting -- greet Ann
```

The first run prints `Hello, Ann!`, from `appsettings.json`.

## Environment variables override the file

An environment variable overrides a value from `appsettings.json`. For a nested key, the separator is a double underscore: `Greeting__Prefix` overrides `Greeting:Prefix`.

```powershell
$env:Greeting__Prefix = "Hey"; dotnet run --project samples/CmdStarter.Samples.Hosting -- greet Ann
```

This run prints `Hey, Ann!`.

## Working folder and console

The sample sets `ContentRootPath = AppContext.BaseDirectory`, so `appsettings.json` is found beside the executable, whatever the working directory. ADR 0015 recommends this setting.

The host's console lifetime is replaced, so the sample's output carries no host status message.

## See also

- Examples: [Configuration](../../samples/CmdStarter.Samples.Configuration), [Hosting](../../samples/CmdStarter.Samples.Hosting).
- Neighbouring pages: [Hosting](hosting.md), [Dependency injection](../concepts/dependency-injection.md), [Binding](../concepts/binding.md), [Testing](testing.md).
- ADR: [0015, Generic Host integration](../adr/0015-generic-host-integration.md), [0006, dependency injection](../adr/0006-dependency-injection.md).
