# CmdStarter samples

These examples are small, runnable programs that show common ways to build command line applications with CmdStarter.

| Example | What it shows | Try it | Guide |
|---|---|---|---|
| [HelloWorld](CmdStarter.Samples.HelloWorld) | A single rooted command with a property option | `dotnet run --project samples/CmdStarter.Samples.HelloWorld -- --to Bob` | [Getting started](../docs/getting-started.md) |
| [TwoCommands](CmdStarter.Samples.TwoCommands) | Two top-level commands with positional arguments | `dotnet run --project samples/CmdStarter.Samples.TwoCommands -- add 2 3` | [Getting started](../docs/getting-started.md) |
| [NestedCommands](CmdStarter.Samples.NestedCommands) | Namespace and attribute based command nesting | `dotnet run --project samples/CmdStarter.Samples.NestedCommands -- remote add origin https://example.com/repo.git` | [Nested commands](../docs/guides/nested-commands.md) |
| [FluentTree](CmdStarter.Samples.FluentTree) | Explicit command tree defined with `Starter.UseTree` | `dotnet run --project samples/CmdStarter.Samples.FluentTree -- users ls` | [Fluent tree](../docs/guides/fluent-tree.md) |
| [DependencyInjection](CmdStarter.Samples.DependencyInjection) | Injecting a service into class and method commands | `dotnet run --project samples/CmdStarter.Samples.DependencyInjection -- greet --to Ann` | [Dependency injection](../docs/concepts/dependency-injection.md) |
| [ScopedServices](CmdStarter.Samples.ScopedServices) | Sharing scoped services and disposing them after execution | `dotnet run --project samples/CmdStarter.Samples.ScopedServices -- run` | [Dependency injection](../docs/concepts/dependency-injection.md) |
| [GlobalOptions](CmdStarter.Samples.GlobalOptions) | Reading a global option from class and method commands | `dotnet run --project samples/CmdStarter.Samples.GlobalOptions -- status --verbose` | [Global options](../docs/guides/global-options.md) |
| [Validation](CmdStarter.Samples.Validation) | Validating option values and required arguments | `dotnet run --project samples/CmdStarter.Samples.Validation -- serve --port 8080` | [Validation](../docs/guides/validation.md) |
| [Configuration](CmdStarter.Samples.Configuration) | Reading environment configuration and overriding it with an option | `dotnet run --project samples/CmdStarter.Samples.Configuration -- show-region --region us-east` | [Configuration](../docs/guides/configuration.md) |
| [Testing](CmdStarter.Samples.Testing) | Reusing application composition with a fake service in tests | `dotnet run --project samples/CmdStarter.Samples.Testing -- transfer alice bob --amount 10` | [Testing](../docs/guides/testing.md) |
| [DevOpsTool](CmdStarter.Samples.DevOpsTool) | Global deployment options, command groups, services, async handlers and exit codes | `dotnet run --project samples/CmdStarter.Samples.DevOpsTool -- service deploy api --version 1.2.3` | [Global options](../docs/guides/global-options.md) |
| [Repl](CmdStarter.Samples.Repl) | Interactive REPL with dependency injection and a scope per command | `dotnet run --project samples/CmdStarter.Samples.Repl` then enter `login ann`, `whoami`, `exit` | [REPL](../docs/guides/repl.md) |
| [CommandStates](CmdStarter.Samples.CommandStates) | Commands and help that follow the application's current session state | `dotnet run --project samples/CmdStarter.Samples.CommandStates` then enter `login ann`, `whoami`, `logout` | [Command states](../docs/guides/command-states.md) |
| [Filters](CmdStarter.Samples.Filters) | Logging and confirmation filters around command execution | `dotnet run --project samples/CmdStarter.Samples.Filters -- deploy --yes` | [Filters](../docs/guides/filters.md) |
| [Hosting](CmdStarter.Samples.Hosting) | Generic Host, appsettings.json, environment variables, options and logging | `dotnet run --project samples/CmdStarter.Samples.Hosting -- greet Ann` | [Hosting](../docs/guides/hosting.md) |
| [ReplServer](CmdStarter.Samples.ReplServer) | An administration console served over a named pipe, one session at a time | `dotnet run --project samples/CmdStarter.Samples.ReplServer -- demo` | [Remote REPL](../docs/guides/remote-repl.md) |
| [Daemon](CmdStarter.Samples.Daemon) | An application that forwards its arguments to a running copy of itself, and runs them locally when none answers | `dotnet run --project samples/CmdStarter.Samples.Daemon -- demo` | [Daemon mode](../docs/guides/daemon-mode.md) |

Run the in-process tests for the Testing example with `dotnet test CmdStarter.sln --filter "FullyQualifiedName~CmdStarter.Samples.Testing.Tests"`.

Every example is also launched as a separate process by `CmdStarter.Tests/Integration/SamplesTests.cs`.
