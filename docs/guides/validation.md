# Validation

Validation rejects a bad value while the command line is parsed, using the DataAnnotations attributes you put on your options, arguments and global options.

## Where validation applies

Each `ValidationAttribute` on an option or an argument becomes a System.CommandLine validator. You can put the attributes on:

- a property of a class command, which becomes an option;
- a property of a global options container, which becomes an option;
- a parameter of a class command's handler, which becomes an argument;
- a parameter of a `[Command]` method, which becomes an argument, or an option when it is marked `[Option]`.

## Attributes shown in this guide

This guide shows four attributes: `[Range]`, `[RegularExpression]`, `[AllowedValues]` and `[Required]`.

`ErrorMessage` replaces the message of the attribute. In this sample, the port is limited to a range, with its own message:

<!-- snippet: samples/CmdStarter.Samples.Validation/Commands/Serve.cs -->
```csharp
public sealed class Serve : StarterCommand
{
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
    public int Port { get; set; } = 8080;
    public override Delegate HandlingMethod => (Action)(() => Console.Out.WriteLine($"Serving on port {Port}"));
}
```

```powershell
dotnet run --project samples/CmdStarter.Samples.Validation -- serve --port 8080
dotnet run --project samples/CmdStarter.Samples.Validation -- serve --port 70000
dotnet run --project samples/CmdStarter.Samples.Validation -- serve --port abc
```

`serve --port 8080` serves on port 8080. `serve --port 70000` is rejected with `Port must be between 1 and 65535.`

Validation runs while System.CommandLine parses the command line, on the converted value. A failed check is a parse error: it is reported as an error, the exit code is not zero, and the handler does not run.

A value that cannot be converted to the property's type, such as `--port abc` for an `int`, is also a parse error reported by System.CommandLine.

## Required options and arguments

`[Required]` on an option property makes the option required, so leaving it out is a parse error. It is also a validation attribute, so an explicitly empty string is rejected too.

<!-- snippet: CmdStarter.Tests.Commands/Options/OptComplete.cs -->
```csharp
[Required]
public int IntOpt { get; set; }
```

A handler parameter without a default value is a required argument. Leaving it out is a parse error, and no attribute is needed. In the sample, `ping` needs its host:

<!-- snippet: samples/CmdStarter.Samples.Validation/Commands/Ping.cs -->
```csharp
public sealed class Ping : StarterCommand
{
    public int Count { get; set; } = 4;
    public override Delegate HandlingMethod
    {
        get => (Action<string>)(host => Console.Out.WriteLine($"Pinging {host} {Count} times"));
    }
}
```

```powershell
dotnet run --project samples/CmdStarter.Samples.Validation -- ping example.com --count 2
dotnet run --project samples/CmdStarter.Samples.Validation -- ping
```

## Regular expressions

`[RegularExpression]` checks a string against a pattern. The pattern `^[a-z]+$` accepts only lowercase letters.

<!-- snippet: CmdStarter.Tests.Commands/Validation/ValidationCommands.cs -->
```csharp
public sealed class CodeCommand : StarterCommand
{
    public override Delegate HandlingMethod => (Func<string, int>)(([RegularExpression("^[a-z]+$")] string code) => 0);
}
```

## Attributes on method parameters

On a `[Command]` method, `[AllowedValues]` limits an argument to a list of values, and `[Range]` on an `[Option]` parameter limits that option. Here `color` accepts `red` or `blue`, and `--size` accepts 1 to 3.

<!-- snippet: CmdStarter.Tests.Commands/Validation/ValidationCommands.cs -->
```csharp
public sealed class PickCommands
{
    [Command]
    public static int Pick(
        [AllowedValues("red", "blue")] string color,
        [Option]
        [Range(1, 3)]
        int size = 1) => size;
}
```

The same attributes work on the properties of a global options container. Here `[Range(0, 3)]` limits `Verbosity`:

<!-- snippet: CmdStarter.Tests.Commands/Validation/ValidationCommands.cs -->
```csharp
public sealed class LimitOptions : IGlobalOptionsContainer
{
    [Range(0, 3)]
    public int Verbosity { get; set; }
}
```

## Options you leave out

An option you leave out is not validated, even when its default value is outside the range. The same value typed on the command line is validated. This applies to option properties and to `[Option]` parameters of methods.

<!-- snippet: CmdStarter.Tests.Commands/Validation/ValidationCommands.cs -->
```csharp
public sealed class ImplicitDefaultCommand : StarterCommand
{
    [Range(1, 3)]
    public int Level { get; set; } = 5;

    public override Delegate HandlingMethod => (Func<int>)(() => Level);
}

public sealed class ImplicitDefaultCommands
{
    [Command]
    public static int Fit([Option][Range(1, 3)] int size = 7) => size;
}
```

## Custom validators

You can add your own System.CommandLine validators once the tree is built. Find the command with `Starter.FindCommand<T>()`, then add the validator to the `Validators` collection of one of its options or arguments. In this example, `LevelCommand` has an option `--level`, and the custom validator refuses every value of it:

<!-- snippet: CmdStarter.Tests/ValidationAttributes.cs -->
```csharp
var starter = CreateStarter();
starter.InstantiateCommands();
var level = starter.FindCommand<LevelCommand>()!.Options.Single(option => option.Name == "--level");
// ...
level.Validators.Add(result => result.AddError("custom validator"));
```

After you add it, `level-command --level 3` is refused, although it was accepted before.

## Limits

Validation runs per value when the line is parsed, before the command instance exists: an attribute that reads other members of the object (`[Compare]`, for instance) is not supported.

## See also

- Examples: [Validation](../../samples/CmdStarter.Samples.Validation).
- Neighbouring pages: [Attributes](../concepts/attributes.md), [Binding](../concepts/binding.md), [Global options](global-options.md), [Errors and exit codes](errors-and-exit-codes.md).
- ADR: [0014, validation attributes](../adr/0014-validation-attributes.md), [0007, parse errors](../adr/0007-parse-errors.md).
