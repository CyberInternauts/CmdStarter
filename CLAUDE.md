# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

CmdStarter is a layer over System.CommandLine (2.0.14 stable) that turns plain C# code into a CLI tree. In the class model, classes become commands, public properties become options and handler parameters become arguments. In the method model, `[Command]` methods become commands. Both models support Microsoft.Extensions.DependencyInjection. The 2.0 rewrite accepts breaking changes (`docs/adr/0005`, `docs/migration/1.x-to-2.0.md`). Target: .NET 10 only (`global.json`, `ProjectsCommon.props`).

## Commands

Run from the repository root (PowerShell; on this machine Git Bash fails to fork subprocesses).

```powershell
dotnet test CmdStarter.sln -nologo                       # build + all tests; the only trustworthy check
dotnet format whitespace --verify-no-changes             # formatting gate (CI runs it)
dotnet test CmdStarter.Tests --filter "FullyQualifiedName~CmdStarter.Tests.Options.IsPropertyFilledWithOption"
dotnet test CmdStarter.Tests --filter "TestCategory=REPL"   # fixtures carry [Category(...)]
```

- Do not chain `dotnet build` then `dotnet test --no-build`: if the build fails, the old binaries are tested and pass.
- `TreatWarningsAsErrors` is on everywhere; the library also runs the `latest-recommended` analyzers. A few CA rules (CA1010, CA1707, CA1715, CA1716, CA1822) are disabled in `.editorconfig` until the public API review.
- Building `CmdStarter.Lib` also produces a NuGet package in `../artefacts` (`GeneratePackageOnBuild`).
- System.CommandLine localizes its help text (French on this machine): tests must check exit codes or symbols, never help wording.

## Architecture

`Starter` (`CmdStarter.Lib/Starter.cs`) runs a lazy three-stage pipeline. Each stage runs once and is invalidated when a filter property changes (`Namespaces`, `Classes` reset everything; `ClassesBuildingMode`, `IsRootingLonelyCommand`, `CommandsTypes` reset the trees):

1. **`FindCommandsTypes`** scans every assembly loaded in the AppDomain for non-abstract classes implementing `IStarterCommand` that have a namespace, then filters them by `Namespaces` (prefix, `~` excludes) and `Classes` (wildcards `?`, `??`, `*`, `**`, `~` excludes; see `Reflection/Helper.cs`).
2. **`BuildTree`** builds a `TreeNode<Type>` with `Loader/CommandsTreeBuilder.cs`, from `[Parent]`/`[Children]` attributes, the namespace hierarchy, or both (`ClassesBuildingMode`). Leftover types become top-level commands.
3. **`InstantiateCommands`** is the adapter. `StarterCommand` no longer derives from `Command`: each command type gets a `Binding/CommandRegistration` that links the type, its instance and a System.CommandLine `Command` it builds. `Reflection/Loader.cs` creates options from properties and arguments from the parameters of `HandlingMethod`. Only leaf commands get an action. With a single command type and `IsRootingLonelyCommand`, its symbols go on the root command.

At execution, the action (`HandleCommandAsync`) binds the global options, then the options onto the instance's properties (`Binding/CommandBinder.cs`). It then invokes the `HandlingMethod` delegate through `DynamicInvoke`. A returned `int`, `Task` or `Task<int>` becomes the exit code. Parse errors return a non-zero code and never throw (`docs/adr/0007`).

Command model:
- A command is a `StarterCommand` subclass or any class implementing `IStarterCommand`. The handler is whatever delegate `HandlingMethod` returns.
- Which properties become options is decided in `Helper.GetProperties`: public read/write properties, filtered by `[Option]`, `[NotOption]` and `[AllOptionsExcluded]`.
- Names are kebab-cased from the type and property names.
- Global options are classes implementing `IGlobalOptionsContainer`. `GlobalOptionsManager` finds them (static cache over all assemblies) and adds them as recursive options on the root. It rebuilds them at each parse; commands read them with `GetGlobalOptions<T>()`.

Instantiation (`docs/adr/0006`): `Starter.Activate` creates every instance. Its priority order is:
1. a static `GetInstance<T>()` declared by the command type;
2. `Starter.ServiceProvider` (Microsoft DI, one `AsyncServiceScope` per execution);
3. the per-`Starter` factory (`SetFactory`, also set by `Start(IServiceManager, …)`);
4. the parameterless constructor.

Each command type gets one **description instance**, created when the tree is built. It is used only to read metadata (name, `HandlingMethod` signature, initial option values), it is released at the end of `InstantiateCommands`, and `FindCommandInstance` returns it. Each execution creates a fresh instance inside `HandleCommandAsync` and releases it with `Binding/CommandActivation` (`await using`), even when the handler throws. `CommandHandled` exposes that instance before it is released. `CmdStarterBuilder` and `CmdStarterApp` wrap all of this for Microsoft DI, with `ValidateScopes` and `ValidateOnBuild` on.

**Method model** (`docs/adr/0009`) runs alongside the class model:
- **Discovery**: `[Command]` (`Attributes/CommandAttribute`) goes on public methods. A class marked `[Command("group")]` becomes a group node under the root; otherwise its methods become top-level commands. Discovery (`Loader/MethodCommandsLoader`) fills `Starter.MethodCommandsTypes`, scanning only assemblies that reference CmdStarter.Lib. `InstantiateCommands` adds these nodes after the class tree and throws `InvalidAttributeException` for invalid definitions.
- **Parameters** are classified when the tree is built (`Binding/ParameterBinding`): `CancellationToken`, then global options containers, then `[FromServices]`, then `[Option]`, then services that `ServiceProvider` reports through `IServiceProviderIsService`; anything else is a positional argument.
- **Execution**: each run gets a fresh declaring-class instance (none for static methods) and, with Microsoft DI, its own scope (`ActivateMethodCommand`). `SetFactory` and `GetInstance` do not apply to this model.
- **Lonely rooting** only happens when there is no method command.

`Repl/ReplStarter` derives from `Starter` and loops over `IReplInputProvider` input, raising `OnCommandExecuted` after each `Start`.

## Tests

- `CmdStarter.Tests` (NUnit) exercises commands defined in `CmdStarter.Tests.Commands`; `CmdStarter.Tests.Common` holds the shared interfaces and the generic `[TestCase<T>]` attributes.
- **The namespaces and attributes of the test commands are test data.** Tree-building tests depend on where a command class lives, so moving or adding a class in `CmdStarter.Tests.Commands` can change other tests' trees.
- Each test isolates its commands by setting `Namespaces`/`Classes` on a fresh `Starter`. `TestsCommon.CreateCmdStarter()` excludes the `Erroneous`, `DepencendyInjection`, `Lifecycle` and `Methods` namespaces by default (exclusions also repeated in `Repl.SetDefaultNamespaces`). The lifecycle and DI tests instead isolate one command type with a `Classes` filter, which makes it a lonely, rooted command.
- Command test classes must expose read-only public properties unless the property is meant to be an option: any setter, even a private one, makes `Helper.GetProperties` turn the property into an option.
- Discovery only sees loaded assemblies, so fixtures call `TestsCommon.GlobalSetup()`, which forces `CmdStarter.Tests.Commands` to load (`DirectoryCatalog`).

## Conventions

- File-scoped namespaces only (`namespace X;`), enforced by `.editorconfig`. Root namespace: `com.cyberinternauts.csharp.<Project>`.
- Ordinal string comparisons in the library.
- Architecture decisions are recorded in `docs/adr/` (index in `docs/adr/README.md`); breaking changes go in `docs/migration/1.x-to-2.0.md`.
- `CmdStarter.Demo.SimpleInjector` still references the published 0.9 NuGet package, not the project.

## Development workflow (local)

The work follows `CmdStarter_plan_strategique_et_technique.md` (untracked), split into lots that Codex executes in the worktree `D:\temp\repos\CmdStarter-codex` (`codex/lot-NNN` branches), each reviewed and then fast-forwarded into `CMD-1-REPL-mode`. Status, handover notes and tracked debt live in `.codex-lots/feuille-de-route.md`, which is excluded locally via `.git/info/exclude`.
