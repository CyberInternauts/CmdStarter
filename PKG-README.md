# CmdStarter

CmdStarter turns plain C# code into a command-line interface built on System.CommandLine 2.0.14, the stable release of that library. Classes become commands and their public properties become options, or methods marked `[Command]` become commands. The library targets .NET 10 only, and supports Microsoft dependency injection in both models.

## Features

- Two command models: classes (inheriting `StarterCommand` or implementing `IStarterCommand`) and `[Command]` methods.
- Discovery by namespaces and class filters, or a command tree described in code with `Starter.UseTree`.
- Options and arguments deduced from the code.
- A Roslyn analyzer that reports definition errors at compile time.
- Global options.
- Validation attributes.
- Command filters.
- Microsoft dependency injection, with one scope per execution.
- A REPL with history and completion.
- Command sequences and text pipes.
- A remote REPL over a named pipe.
- Command states.
- Generic Host integration.
- Trimming and Native AOT, for an application that has the command catalog that the package generates.
- Readable error messages and consistent exit codes.

<!-- snippet: samples/CmdStarter.Samples.HelloWorld/Commands/Hello.cs -->
```csharp
using CyberInternauts.CmdStarter;

public sealed class Hello : StarterCommand
{
    public string To { get; set; } = "World";

    public override Delegate HandlingMethod => (Action)(() => Console.Out.WriteLine($"Hello, {To}!"));
}
```

## Documentation

- [Documentation index](https://github.com/CyberInternauts/CmdStarter/blob/master/docs/index.md)
- [Getting started](https://github.com/CyberInternauts/CmdStarter/blob/master/docs/getting-started.md)
- [Samples](https://github.com/CyberInternauts/CmdStarter/blob/master/samples/README.md)
