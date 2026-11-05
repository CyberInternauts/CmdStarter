# CmdStarter compared

This page compares CmdStarter with three other .NET libraries for command-line applications: Spectre.Console.Cli, CliFx and Cocona. It states facts, not judgements of quality.

Every fact about another library comes from its package on nuget.org, its README, its documentation or its repository, read on 2026-11-05; the sources are listed at the end, and each cell gives the keys of its sources in brackets. *not in the sources read* means that the README, documentation and package read do not mention it: it does not mean that the library lacks the feature. Every CmdStarter cell links to the page that describes it.

## Versions compared

| Library | Version | Published on nuget.org |
| --- | --- | --- |
| CmdStarter (`cints.CmdStarter`) | 1.0.0 | the release that this documentation describes |
| Spectre.Console.Cli | 0.57.2, the last stable version; the last version listed is the pre-release 1.0.0-alpha.0.16 [S1] | 2026-09-29 [S3] |
| CliFx | 3.0.1 [F1] | 2026-08-31 [F3] |
| Cocona | 2.2.0 [K1] | 2023-03-27 [K3] |
| Cocona.Lite | 2.2.0 [K4] | 2023-03-27 [K6] |

The README of Cocona describes Cocona.Lite as "a lightweight version of Cocona" without the `Microsoft.Extensions.*` dependencies [K8]. The two share the Cocona column below; a cell names Cocona.Lite where its sources tell them apart.

## Comparison

| Topic | CmdStarter 1.0.0 | Spectre.Console.Cli 0.57.2 | CliFx 3.0.1 | Cocona 2.2.0, Cocona.Lite 2.2.0 |
| --- | --- | --- | --- | --- |
| Target frameworks | .NET 10 only, `net10.0` ([Getting started](getting-started.md)) | .NET Standard 2.0, net8.0, net9.0, net10.0 [S2] | .NET Standard 2.0 and 2.1, net6.0, net7.0, net10.0 [F2] | .NET Standard 2.0 and 2.1, net6.0 [K2, K5]; the README names .NET 6 as "Required to use Minimal API" [K8] |
| Parser | System.CommandLine 2.0.14, which does the parsing, the help and the completions ([System.CommandLine](advanced/system-command-line.md)) | its own: "a modern library for parsing command line arguments" [S4]; depends on Spectre.Console [S2] | its own: "a variation of the POSIX argument syntax" [F4]; no package dependency, except System.Threading.Tasks.Extensions on .NET Standard 2.0 [F2] | Cocona.Core, which does not depend on System.CommandLine [K7]; Cocona also depends on Microsoft.Extensions.DependencyInjection, Hosting and Logging 6.0.0, Cocona.Lite only on Cocona.Core [K2, K5] |
| How a command is declared | a class that derives from `StarterCommand` or implements `IStarterCommand`, a method marked `[Command]`, or a plain class marked `[Command]` ([Commands](concepts/commands.md)) | a class that derives from `AsyncCommand<TSettings>`, whose input is a class that derives from `CommandSettings` [S8, S11] | a `partial` type marked with the `Command` attribute that implements `ICommand` [F4] | public methods of a class, the `Command` attribute, or lambdas ("ASP.NET Core-like Minimal API") [K8] |
| Options and arguments | public read/write properties are options, and their initial value is the default shown in the help; handler parameters are arguments, and a parameter with a default value is optional ([Binding](concepts/binding.md)) | properties of the settings class with the `CommandArgument` and `CommandOption` attributes; defaults with `[DefaultValue]`, shown in the help [S7, S8] | properties with the `CommandParameter` and `CommandOption` attributes; the default is the property initializer; an option can fall back on an environment variable [F4] | method parameters are options; "Command-line arguments are defined as method parameters as same as options"; a nullable or optional parameter is optional; parameter sets (`ICommandParameterSet`) [K8] |
| Nested commands and groups | by namespace, with `[Parent<T>]` and `[Children]`, with groups and space-separated paths in the method model, or in code with `Starter.UseTree` ([Nested commands](guides/nested-commands.md), [Fluent command tree](guides/fluent-tree.md)) | `AddBranch`; branches can contain branches, and a branch can have settings that its subcommands inherit [S9] | "Commands that have common name segments are considered to be hierarchically related" [F4] | sub-commands, and nested sub-commands with `AddSubCommand` [K8] |
| Dependency injection | Microsoft.Extensions.DependencyInjection, with one scope per execution; another container through `Starter.SetFactory` ([Dependency injection](concepts/dependency-injection.md)) | through `ITypeRegistrar` and `ITypeResolver`; the tutorial has the application write the bridge to Microsoft.Extensions.DependencyInjection; scope per execution: *not in the sources read* [S10] | `UseTypeInstantiator(...)`, with Microsoft.Extensions.DependencyInjection as the example; without it, "only types with public parameter-less constructors are supported"; scope per execution: *not in the sources read* [F4] | constructor parameters and parameters marked with the `FromService` attribute, from `IServiceProvider`; scope per execution: *not in the sources read*. Cocona.Lite offers "The minimal Dependency Injection function" of its own and does not depend on the Microsoft.Extensions packages [K5, K8] |
| Generic Host integration | the `cints.CmdStarter.Hosting` package, `IHostApplicationBuilder.AddCmdStarter` and `IHost.RunCmdStarterAsync` ([Hosting](guides/hosting.md)) | *not in the sources read* | *not in the sources read* | "Cocona can be integrated with GenericHost of Microsoft.Extensions.Hosting" [K8]; not Cocona.Lite, which has "No `Microsoft.Extensions.*` dependencies" [K8] |
| Validation | DataAnnotations attributes on options and arguments ([Validation](guides/validation.md)) | `Validate()` of the settings class, which returns a `ValidationResult`, and attributes that derive from `ParameterValidationAttribute` [S8] | validators that implement `IInputValidator<T>` [F4] | attributes, including those of `System.ComponentModel.DataAnnotations` [K8] |
| Help and completion | help and completions by System.CommandLine; completion values with `[AutoComplete]`; Tab completion in the REPL ([System.CommandLine](advanced/system-command-line.md), [Attributes](concepts/attributes.md), [REPL](guides/repl.md)) | `-h` / `--help` on every command; a custom `IHelpProvider`; shell completion: *not in the sources read* [S12, S13] | "Comprehensive auto-generated help text"; completion: *not in the sources read* [F4] | built-in help; shell completion for `bash` and `zsh`, "**disabled** by default" [K8] |
| Cancellation (Ctrl+C) | the token given to `Start` reaches the handler, and a cancelled command returns 130; in the REPL, Ctrl+C cancels only the running command ([Errors and exit codes](guides/errors-and-exit-codes.md), [REPL](guides/repl.md)) | the token given to `RunAsync` reaches `ExecuteAsync`; in the example, the application connects `Console.CancelKeyPress` itself [S11] | "You can make a command cancellation-aware by calling `console.RegisterCancellationHandler()`" [F4] | a `CancellationToken` from `CoconaAppContext` or `ICoconaAppContextAccessor`; Ctrl+C: *not in the sources read* [K8] |
| Trimming and Native AOT | supported for an application that has the command catalog, and measured on win-x64, trimmed and Native AOT, with four applications ([Trimming](advanced/trimming.md), [Native AOT](advanced/native-aot.md)) | the project file sets `IsAotCompatible` and `IsTrimmable` to `false` for its .NET targets [S5] | the README states: "Compatible with AOT compilation and trimming"; measurement: *not in the sources read* [F4] | *not in the sources read* |
| Source generator | generates the command catalog and the typed symbol factories ([Source generator](advanced/source-generator.md)) | *not in the sources read* | "uses source generators to discover commands and produce the `Main()` method automatically" [F4] | *not in the sources read* |
| Testing support | execution in memory with its exit code; a handler's output captured through `CommandConsole` ([Testing](guides/testing.md), [Command console](guides/command-console.md)) | `CommandAppTester` from the package Spectre.Console.Cli.Testing "runs commands in-memory and captures exit codes and output" [S14] | `FakeConsole` and `FakeInMemoryConsole`, with the output read back by `ReadOutputString()` [F4] | *not in the sources read* |
| Console rendering (tables, colors, prompts) | no rendering of its own: a handler writes text to its `CommandConsole` ([Command console](guides/command-console.md)) | through Spectre.Console, a separate package that it depends on: tables, trees, progress, prompts, live displays and "Over forty renderable widgets" [S2, S15] | a console abstraction to "write text, read binary data, or otherwise interact with the console"; rendering: *not in the sources read* [F4] | *not in the sources read* |
| Localization of help | by System.CommandLine, which follows the language of the machine ([System.CommandLine](advanced/system-command-line.md)) | *not in the sources read* | *not in the sources read* | `ICoconaLocalizer` localizes command descriptions, and Microsoft.Extensions.Localization can be used [K8] |
| License | MIT ([README](../README.md)) | MIT [S2] | MIT [F2] | MIT [K2, K5] |

## Where the others are stronger

- **Frameworks.** The three others target .NET Standard 2.0, so they also run where .NET 10 is not available [S2, F2, K2]; CmdStarter targets .NET 10 only.
- **Console rendering.** Spectre.Console.Cli comes with Spectre.Console, whose tables, trees, progress bars, prompts and widgets a command can use [S15]. CmdStarter writes plain text.
- **Shell completion and localized descriptions.** Cocona documents shell completion for `bash` and `zsh`, and `ICoconaLocalizer` for command descriptions [K8]. CmdStarter's pages describe neither.
- **Dependencies.** CliFx has no package dependency beyond System.Threading.Tasks.Extensions on .NET Standard 2.0 [F2], and Cocona.Lite depends only on Cocona.Core [K5]. CmdStarter depends on System.CommandLine and on Microsoft.Extensions.DependencyInjection.
- **Lambdas as commands.** Cocona accepts a lambda as a command, in the style of ASP.NET Core minimal APIs [K8].
- **Environment variables.** CliFx lets an option fall back on an environment variable [F4].

## Where CmdStarter is different

- **A REPL** with history and Tab completion, in which Ctrl+C cancels only the running command ([REPL](guides/repl.md)).
- **Sequences and pipes** on one line: `a; b`, `a && b`, `a || b` and `a | b` ([Sequences and pipes](guides/sequences-and-pipes.md)).
- **A remote REPL and a daemon mode** over a named pipe ([Remote REPL](guides/remote-repl.md), [Daemon mode](guides/daemon-mode.md)).
- **Trimming and Native AOT measured**: the criterion is that a published application does what it does on CoreCLR, and the measured runs are listed ([Trimming](advanced/trimming.md), [Native AOT](advanced/native-aot.md)).
- **Three ways to declare a command**: a class, a method, or a plain class with no base class and no interface ([Commands](concepts/commands.md)).
- **Compile-time analyzers**: 17 rules, `CMDS0001` to `CMDS0017`, report definition mistakes at build time ([Analyzers](advanced/analyzers.md)).
- **Command states**, which hide and refuse commands ([Command states](guides/command-states.md)), and **command filters** around every execution ([Filters](guides/filters.md)).
- **Built on System.CommandLine**, so the parsing, the help and the completions are those of System.CommandLine ([System.CommandLine](advanced/system-command-line.md)).

## Sources

All read on 2026-11-05.

Spectre.Console.Cli:

- [S1] https://api.nuget.org/v3-flatcontainer/spectre.console.cli/index.json
- [S2] https://api.nuget.org/v3-flatcontainer/spectre.console.cli/0.57.2/spectre.console.cli.nuspec
- [S3] https://api.nuget.org/v3/registration5-semver1/spectre.console.cli/index.json
- [S4] https://raw.githubusercontent.com/spectreconsole/spectre.console.cli/main/README.md
- [S5] https://raw.githubusercontent.com/spectreconsole/spectre.console.cli/main/src/Spectre.Console.Cli/Spectre.Console.Cli.csproj
- [S6] https://spectreconsole.net/cli
- [S7] https://spectreconsole.net/cli/how-to/defining-commands-and-arguments/
- [S8] https://spectreconsole.net/cli/reference/attribute-and-parameter-reference/
- [S9] https://spectreconsole.net/cli/how-to/working-with-multiple-command-hierarchies/
- [S10] https://spectreconsole.net/cli/tutorials/dependency-injection-in-cli-apps/
- [S11] https://spectreconsole.net/cli/how-to/async-commands-and-cancellation/
- [S12] https://spectreconsole.net/cli/how-to/customizing-help-text-and-usage/
- [S13] https://spectreconsole.net/cli/reference/built-in-command-behaviors/
- [S14] https://spectreconsole.net/cli/how-to/testing-command-line-applications/
- [S15] https://spectreconsole.net

CliFx:

- [F1] https://api.nuget.org/v3-flatcontainer/clifx/index.json
- [F2] https://api.nuget.org/v3-flatcontainer/clifx/3.0.1/clifx.nuspec
- [F3] https://api.nuget.org/v3/registration5-semver1/clifx/index.json
- [F4] https://raw.githubusercontent.com/Tyrrrz/CliFx/master/Readme.md

Cocona and Cocona.Lite:

- [K1] https://api.nuget.org/v3-flatcontainer/cocona/index.json
- [K2] https://api.nuget.org/v3-flatcontainer/cocona/2.2.0/cocona.nuspec
- [K3] https://api.nuget.org/v3/registration5-semver1/cocona/index.json
- [K4] https://api.nuget.org/v3-flatcontainer/cocona.lite/index.json
- [K5] https://api.nuget.org/v3-flatcontainer/cocona.lite/2.2.0/cocona.lite.nuspec
- [K6] https://api.nuget.org/v3/registration5-semver1/cocona.lite/index.json
- [K7] https://api.nuget.org/v3-flatcontainer/cocona.core/2.2.0/cocona.core.nuspec
- [K8] https://raw.githubusercontent.com/mayuki/Cocona/master/README.md

## See also

- [Getting started](getting-started.md)
- [Commands](concepts/commands.md)
- [System.CommandLine](advanced/system-command-line.md)
- [Trimming](advanced/trimming.md) and [Native AOT](advanced/native-aot.md)
