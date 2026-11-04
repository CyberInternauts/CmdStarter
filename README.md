# CmdStarter

CmdStarter turns plain C# code into a command-line interface built on System.CommandLine 2.0.14, the stable release of that library. Classes become commands and their public properties become options, or methods marked `[Command]` become commands. The library targets .NET 10 only, and supports Microsoft dependency injection in both models.

## Links

- Article: https://www.codeproject.com/Articles/5361921/Integrate-System-CommandLine-Easily-with-CmdStarte
- Nuget package: https://www.nuget.org/packages/cints.CmdStarter
- Jira: https://cyberinternauts.atlassian.net/browse/CMD

## Features

- Two command models: classes (inheriting `StarterCommand` or implementing `IStarterCommand`) and `[Command]` methods. See [Commands](docs/concepts/commands.md).
- Discovery by namespaces and class filters, or a command tree described in code with `Starter.UseTree`. See [Discovery](docs/concepts/discovery.md) and [Fluent command tree](docs/guides/fluent-tree.md).
- Options and arguments deduced from the code. See [Binding](docs/concepts/binding.md).
- A Roslyn analyzer that reports definition errors at compile time. See [Analyzers](docs/advanced/analyzers.md).
- Global options. See [Global options](docs/guides/global-options.md).
- Validation attributes. See [Validation](docs/guides/validation.md).
- Command filters. See [Filters](docs/guides/filters.md).
- Microsoft dependency injection, with one scope per execution. See [Dependency injection](docs/concepts/dependency-injection.md).
- A REPL with history and completion. See [REPL](docs/guides/repl.md).
- Command sequences and text pipes. See [Sequences and pipes](docs/guides/sequences-and-pipes.md).
- A remote REPL over a named pipe. See [Remote REPL](docs/guides/remote-repl.md).
- Command states. See [Command states](docs/guides/command-states.md).
- Generic Host integration. See [Hosting](docs/guides/hosting.md).
- Readable error messages and consistent exit codes. See [Errors and exit codes](docs/guides/errors-and-exit-codes.md).

## Getting started

Install the package. It is a pre-release, so `--prerelease` is needed:

```powershell
dotnet add package cints.CmdStarter --prerelease
```

The project must target `net10.0`. A command is a class that derives from `StarterCommand`. Its public read/write property `To` becomes the option `--to`, and `HandlingMethod` is the handler:

<!-- snippet: samples/CmdStarter.Samples.HelloWorld/Commands/Hello.cs -->
```csharp
using CyberInternauts.CmdStarter;

public sealed class Hello : StarterCommand
{
    public string To { get; set; } = "World";

    public override Delegate HandlingMethod => (Action)(() => Console.Out.WriteLine($"Hello, {To}!"));
}
```

The program's entry point only passes its arguments to `new Starter().Start(args)` and returns its exit code. Run the sample with `dotnet run --project samples/CmdStarter.Samples.HelloWorld -- --to Bob`, which prints `Hello, Bob!`.

Next steps:
- [Documentation index](docs/index.md)
- [Getting started](docs/getting-started.md)
- [Samples](samples/README.md)
- [Migration from 1.x to 2.0](docs/migration/1.x-to-2.0.md)

## Participation | Submit issues, ideas

The project uses a free open source licence of Jira to manage its development.

https://cyberinternauts.atlassian.net/browse/CMD

If you want to participate, there are two options:
- Fork this repository and submit a pull request
- Ask to join the team

## License

MIT License. See [LICENSE.txt](https://github.com/CyberInternauts/CmdStarter/blob/master/LICENSE.txt)

## Collaborators

- [Jonathan Boivin](https://github.com/djon2003): Project leader, main developer, reviewer
- [Norbert Ormándi](https://github.com/DeszkaCodes): Developer, reviewer. A special thank to him to have believed in this project!
