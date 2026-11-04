# Analyzers

CmdStarter ships a Roslyn analyzer. At compile time, it reports mistakes that CmdStarter would otherwise find when it builds the command tree, and some advice that has no runtime counterpart. This page lists the rules and how to change their severity.

## Installation

The analyzer ships in the main package, `cints.CmdStarter`, under `analyzers/dotnet/cs`, with no extra package dependency. The same assembly holds the generator of the command catalog, described in [Source generator](source-generator.md).

## The rules

There are 15 rules, `CMDS0001` to `CMDS0015`, in the category Usage. Rules `CMDS0001` to `CMDS0011` repeat, at compile time, checks that CmdStarter makes when it builds the command tree. Rules `CMDS0012` and `CMDS0013` are advice. Rules `CMDS0014` and `CMDS0015` are reported by the catalog generator.

| Id | Title | What is reported |
| --- | --- | --- |
| `CMDS0001` | Parent cycle | A class whose `[Parent]` chain comes back to itself, on each class of the cycle |
| `CMDS0002` | Multiple parent attributes | A class with more than one `[Parent]` attribute, on every attribute after the first |
| `CMDS0003` | Auto-complete has no values | An `[AutoComplete]` that cannot give any value, such as blank text only |
| `CMDS0004` | Unsupported auto-complete type | An `[AutoComplete]` source type that is not an enum, an `IAutoCompleteProvider` or an `IAutoCompleteFactory` |
| `CMDS0005` | Empty command name | A blank or `null` constant name in `[Command]`, or passed to the `StarterCommand` base constructor |
| `CMDS0006` | Command method is not public | A non-public method marked `[Command]` |
| `CMDS0007` | Command method is generic | A generic method marked `[Command]` |
| `CMDS0008` | Command parameter is by reference | A `ref`, `out` or `in` parameter of a `[Command]` method, for each such parameter |
| `CMDS0009` | Mixed command models | A type that implements `IStarterCommand` and also declares a `[Command]` method |
| `CMDS0010` | Abstract instance command | An instance `[Command]` method in an abstract class (a static one is fine) |
| `CMDS0011` | Empty command group | A class marked `[Command]` that declares no `[Command]` method |
| `CMDS0012` | Global option is hidden | A command option (property, or `[Option]` parameter of a method) with the same name or alias as a global option |
| `CMDS0013` | Empty global options container | A non-abstract global options container that has no option property |
| `CMDS0014` | Type is not in the command catalog | A type that the generator leaves out of the catalog, with the reason (severity `Info`) |
| `CMDS0015` | `HandlingMethod` cannot be read at compile time | An overridden `HandlingMethod` whose delegate type the generator cannot read, reported on the property (severity `Info`); see [Symbol factories](source-generator.md#symbol-factories) |

Each diagnostic message names the type, method, parameter or option concerned.

## Severity

Rules `CMDS0001` to `CMDS0013` have the severity `Warning`. `CMDS0014` and `CMDS0015` have the severity `Info`.

Like any Roslyn diagnostic, each rule's severity can be raised or lowered, for example in `.editorconfig`. The following lines make `CMDS0001` an error and turn `CMDS0012` off:

```ini
[*.cs]
dotnet_diagnostic.CMDS0001.severity = error
dotnet_diagnostic.CMDS0012.severity = none
```

Because the rules are warnings, a project that sets `TreatWarningsAsErrors` fails to build when one of them is reported.

## What stays at runtime

The analyzer does not report the cases that depend on the command tree or on the services at runtime:

- name or alias conflicts between different class commands;
- a state provider that is missing when `[AvailableInState]` is used;
- a `[FromServices]` parameter when there is no service provider;
- a global option declared in another assembly: `CMDS0012` only sees the global options containers of the project being compiled.

CmdStarter reports them when it builds the tree or runs a command, as described in [Errors and exit codes](../guides/errors-and-exit-codes.md). A parent cycle, for example, is also found at runtime, as a `LoopException`, and a second `[Parent]` attribute as an `InvalidAttributeException`.

Generated code is not analyzed.

## See also

- Neighbouring pages: [Errors and exit codes](../guides/errors-and-exit-codes.md), [Attributes](../concepts/attributes.md), [Commands](../concepts/commands.md), [Global options](../guides/global-options.md).
- ADR: [0018, Roslyn analyzers](../adr/0018-roslyn-analyzers.md).
