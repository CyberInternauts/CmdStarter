# CmdStarter samples

These examples are small, runnable programs that show common ways to build command line applications with CmdStarter.

| Example | What it shows | Try it |
|---|---|---|
| [HelloWorld](CmdStarter.Samples.HelloWorld) | A single rooted command with a property option | `dotnet run --project samples/CmdStarter.Samples.HelloWorld -- --to Bob` |
| [TwoCommands](CmdStarter.Samples.TwoCommands) | Two top-level commands with positional arguments | `dotnet run --project samples/CmdStarter.Samples.TwoCommands -- add 2 3` |
| [NestedCommands](CmdStarter.Samples.NestedCommands) | Namespace and attribute based command nesting | `dotnet run --project samples/CmdStarter.Samples.NestedCommands -- remote add origin https://example.com/repo.git` |
| [DependencyInjection](CmdStarter.Samples.DependencyInjection) | Injecting a service into class and method commands | `dotnet run --project samples/CmdStarter.Samples.DependencyInjection -- greet --to Ann` |
| [ScopedServices](CmdStarter.Samples.ScopedServices) | Sharing scoped services and disposing them after execution | `dotnet run --project samples/CmdStarter.Samples.ScopedServices -- run` |
| [GlobalOptions](CmdStarter.Samples.GlobalOptions) | Reading a global option from class and method commands | `dotnet run --project samples/CmdStarter.Samples.GlobalOptions -- status --verbose` |
| [Validation](CmdStarter.Samples.Validation) | Validating option values and required arguments | `dotnet run --project samples/CmdStarter.Samples.Validation -- serve --port 8080` |
| [Configuration](CmdStarter.Samples.Configuration) | Reading environment configuration and overriding it with an option | `dotnet run --project samples/CmdStarter.Samples.Configuration -- show-region --region us-east` |
| [Testing](CmdStarter.Samples.Testing) | Reusing application composition with a fake service in tests | `dotnet run --project samples/CmdStarter.Samples.Testing -- transfer alice bob --amount 10` |
| [DevOpsTool](CmdStarter.Samples.DevOpsTool) | Global deployment options, command groups, services, async handlers and exit codes | `dotnet run --project samples/CmdStarter.Samples.DevOpsTool -- service deploy api --version 1.2.3` |
| [Repl](CmdStarter.Samples.Repl) | Interactive REPL with dependency injection and a scope per command | `dotnet run --project samples/CmdStarter.Samples.Repl` then enter `login ann`, `whoami`, `exit` |

Run the in-process tests for the Testing example with `dotnet test CmdStarter.sln --filter "FullyQualifiedName~CmdStarter.Samples.Testing.Tests"`.

Every example is also launched as a separate process by `CmdStarter.Tests/Integration/SamplesTests.cs`.
