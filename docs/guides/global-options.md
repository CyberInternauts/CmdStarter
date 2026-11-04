# Global options

Global options are options that every command accepts, and this guide shows how to declare them in a class, read them from a class command or from a method, and what values they hold.

## Declare a container

A global options container is a class that implements `IGlobalOptionsContainer`, an empty marker interface. Its public read-write properties become options of the root command, and they are recursive: every command accepts them, so `status --verbose` works on any command.

<!-- snippet: samples/CmdStarter.Samples.GlobalOptions/Options/OutputOptions.cs -->
```csharp
public sealed class OutputOptions : IGlobalOptionsContainer
{
    public bool Verbose { get; set; }
}
```

Option names follow the property names in kebab-case: `Verbose` becomes `--verbose`, `DryRun` becomes `--dry-run`, and `Environment` becomes `--environment`. The same rules as for command options apply to a container, including `[Description]`, `[Hidden]`, the attributes that select properties (`[Option]`, `[NotOption]`, `[AllOptionsExcluded]`), the validation attributes, and the initial value of a property, which becomes its default.

## Read a container in a class command

A class command reads a container with `GetGlobalOptions<T>()`. It returns `null` when that container is not available, which is why the sample uses `?.`.

<!-- snippet: samples/CmdStarter.Samples.GlobalOptions/Commands/Status.cs -->
```csharp
public sealed class Status : StarterCommand
{
    public override Delegate HandlingMethod => (Action)(() =>
    {
        Console.Out.WriteLine("status: ok");
        if (GetGlobalOptions<Options.OutputOptions>()?.Verbose == true)
        {
            Console.Out.WriteLine("details: all systems nominal");
        }
    });
}
```

```powershell
dotnet run --project samples/CmdStarter.Samples.GlobalOptions -- status --verbose
```

The DevOpsTool `status` command reads its container, `DeployOptions`, the same way, and falls back on `dev` when the container is missing.

<!-- snippet: samples/CmdStarter.Samples.DevOpsTool/Commands/Status.cs -->
```csharp
public sealed class Status(IDeploymentClient client) : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() =>
    {
        Console.Out.WriteLine($"Environment: {GetGlobalOptions<DeployOptions>()?.Environment ?? "dev"}");
        // ...
    });
}
```

```powershell
dotnet run --project samples/CmdStarter.Samples.DevOpsTool -- status --environment staging
```

## Read a container in a method

A method command receives a container as a parameter of the container's type, with no attribute. The parameter is not an option or an argument of the command.

<!-- snippet: samples/CmdStarter.Samples.GlobalOptions/Commands/InfoCommands.cs -->
```csharp
public sealed class InfoCommands
{
    [Command]
    public static void Info(OutputOptions options)
    {
        Console.Out.WriteLine("info: v1");
        if (options.Verbose) Console.Out.WriteLine("details: build 42");
    }
}
```

The `service deploy` method of DevOpsTool takes `DeployOptions` in the same way.

## What the container holds

A container holds the values of the current execution. An option that is not on the command line keeps the initial value of its property. In the DevOpsTool sample, `Environment` starts at `"dev"`, so without `--environment` the deployment goes to `dev`. The `DryRun` property starts at `false`.

<!-- snippet: samples/CmdStarter.Samples.DevOpsTool/Options/DeployOptions.cs -->
```csharp
public sealed class DeployOptions : IGlobalOptionsContainer
{
    public string Environment { get; set; } = "dev";

    public bool DryRun { get; set; }
}
```

```powershell
dotnet run --project samples/CmdStarter.Samples.DevOpsTool -- service deploy api --version 1.2.3 --environment prod --dry-run
```

## Which containers are found

Containers are found in the assemblies loaded in the process, and they are filtered like commands, by `Starter.Assemblies`, `Starter.Namespaces` and `Starter.Classes`. If `Classes` names a command but not the container, the container is left out, so name both. The containers found are sorted by full name.

## Read the parsed container after a run

After a command has run, `Starter.GlobalOptionsManager.GetGlobalOptions<T>()` gives you the container as it was parsed.

## See also

- Examples: [GlobalOptions](../../samples/CmdStarter.Samples.GlobalOptions), [DevOpsTool](../../samples/CmdStarter.Samples.DevOpsTool).
- Neighbouring pages: [Binding](../concepts/binding.md), [Attributes](../concepts/attributes.md), [Validation](validation.md), [Commands](../concepts/commands.md).
- ADR: [0003, internal command binder](../adr/0003-internal-command-binder.md), [0014, validation attributes](../adr/0014-validation-attributes.md).
