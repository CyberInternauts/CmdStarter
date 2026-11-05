# 0018. Roslyn Analyzers

Status: Accepted
Date: 2026-11-03

## Context

Several command-definition errors are detected only when CmdStarter builds the command tree or creates its symbols. Depending on the application filters, a faulty class may not be reached until a particular filter selects it. This delays feedback until runtime for problems that can be recognized from the compilation.

## Decision

Add a Roslyn analyzer project targeting `netstandard2.0` and compiled against `Microsoft.CodeAnalysis.CSharp` 5.0.0. Ship its assembly in the main `cints.CmdStarter` package under `analyzers/dotnet/cs`, without adding an analyzer dependency to the package.

Every diagnostic from CMDS0001 through CMDS0011 reproduces a validation already performed at runtime. CMDS0012 and CMDS0013 are user-requested advice and have no corresponding runtime validation.

| Rule | Diagnostic |
| --- | --- |
| CMDS0001 | Reports each class whose `[Parent]` chain returns to that class; runtime `LoopException` in `AddParentsToTree`. |
| CMDS0002 | Reports a class with multiple `[Parent]` attributes, including derived attributes; runtime `InvalidAttributeException` in `GetParentAttribute`. |
| CMDS0003 | Reports `[AutoComplete]` that cannot produce any completion values; runtime `NotSupportedException` from the `AutoCompleteAttribute` constructors. |
| CMDS0004 | Reports an auto-complete source type that is neither an enum nor an `IAutoCompleteProvider` or `IAutoCompleteFactory`; runtime `NotSupportedException`. |
| CMDS0005 | Reports a blank `[Command]` name or a constant blank name passed to `StarterCommand(string, string?)`; runtime `InvalidAttributeException` or `ArgumentException`. |
| CMDS0006 | Reports a non-public method marked `[Command]`; runtime validation in `MethodCommandsLoader.Validate`. |
| CMDS0007 | Reports a generic method marked `[Command]`; runtime validation in `MethodCommandsLoader.Validate`. |
| CMDS0008 | Reports a `ref`, `out`, or `in` parameter on a method marked `[Command]`; runtime validation in `MethodCommandsLoader.Validate`. |
| CMDS0009 | Reports a type that implements `IStarterCommand` and declares a `[Command]` method; runtime validation in `MethodCommandsLoader.Validate`. |
| CMDS0010 | Reports an instance `[Command]` method declared by a non-static abstract class; runtime validation in `MethodCommandsLoader.Validate`. |
| CMDS0011 | Reports a class marked `[Command]` that declares no `[Command]` method and has no handler (see the amendment below); runtime validation in `MethodCommandsLoader`. |
| CMDS0012 | Advises when an option on a command shares a name or alias with a global option and therefore hides it; requested in ADR 0010, not checked at runtime. |
| CMDS0013 | Advises when a non-abstract global options container contributes no option properties; requested by Jira CMD-16, not checked at runtime. |

Amended on 2026-11-05 (lot 037): a class marked `[Command]` that declares no `[Command]` method is a plain command ([ADR 0032](0032-plain-class-commands.md)). CMDS0011 is retitled "Command class without a handler" ("Type '{0}' must declare command methods or exactly one public instance method") and reports such a class that has no public instance method of its own, or several, without `Handler`, and a static, abstract or generic one; CMDS0009 also reports a class that implements `IStarterCommand` and is itself marked `[Command]`; CMDS0007 and CMDS0008 also apply to the handler of a plain command; CMDS0012 also covers its properties and the `[Option]` parameters of its handler; and CMDS0017 "Invalid command handler" is added for a `Handler` that names no public instance method, several, or that is set on a method, on a class of command methods or on a class that cannot be a plain command. CMDS0017 reproduces a validation of the runtime too.

All rules have `Warning` severity. This lets a potential false positive remain visible without blocking compilation; an application can raise the severity in `.editorconfig`.

## Consequences

The analyzer reports source-visible definition problems at compile time, even when a command would otherwise be skipped by a runtime filter. Runtime validation remains necessary for conflicts between names in different class commands, state providers, and `[FromServices]` parameters without a service provider. Those conditions depend on the command tree or runtime services and are not diagnosed by these rules.
