# 0013. Public API Names

Status: Accepted
Date: 2026-11-03

Amendment (2026-11-04): `IServiceManager` and the `Start(IServiceManager, …)` overloads are removed before 1.0 ships (lot 035); the obsolete period described below never reached a release.

## Context

CmdStarter 2.0 accepts public API breaking changes. The previous Java-style namespaces and underscore-separated public constants conflict with .NET naming rules, and `IServiceManager` duplicates the newer provider and factory configuration paths.

## Decision

The public namespaces use `CyberInternauts.CmdStarter` and its child namespaces, such as `CyberInternauts.CmdStarter.Attributes`, `CyberInternauts.CmdStarter.Interfaces`, and `CyberInternauts.CmdStarter.Repl`. Project namespaces use `CyberInternauts.CmdStarter.<Project>`, including `CyberInternauts.CmdStarter.Demo.SimpleInjector`.

The assembly remains `CmdStarter.Lib.dll`, and the NuGet package identifier remains `cints.CmdStarter`.

Public constants use PascalCase:

| Before | After |
| --- | --- |
| `Starter.EXCLUSION_SYMBOL` | `Starter.ExclusionSymbol` |
| `Starter.ANY_CHAR_SYMBOL` | `Starter.AnyCharSymbol` |
| `Starter.ANY_CHAR_SYMBOL_INCLUDE_DOTS` | `Starter.AnyCharSymbolIncludeDots` |
| `Starter.MULTI_ANY_CHAR_SYMBOL` | `Starter.MultiAnyCharSymbol` |
| `Starter.MULTI_ANY_CHAR_SYMBOL_INCLUDE_DOTS` | `Starter.MultiAnyCharSymbolIncludeDots` |
| `StarterCommand.OPTION_PREFIX` | `StarterCommand.OptionPrefix` |
| `StarterCommand.DESCRIPTION_JOINER` | `StarterCommand.DescriptionJoiner` |
| `AliasAttribute.DEFAULT_PREFIX` | `AliasAttribute.DefaultPrefix` |
| `IStarterCommand.EMPTY_EXECUTION` | `IStarterCommand.EmptyExecution` |

`VisitCommands(Command, Action<Command>)` is static. The instance overload `VisitCommands(Action<Command>)` remains.

All analyzer rules remain active, including CA1707, CA1716, and CA1822. `.editorconfig` contains no analyzer severity exceptions.

`IServiceManager` and the `Starter.Start(IServiceManager, ...)` overloads are obsolete. They are planned for removal in 3.0. Use `Starter.ServiceProvider` or `CmdStarterBuilder` for Microsoft.Extensions.DependencyInjection, and `Starter.SetFactory` for another container.

## Consequences

Applications migrating to 2.0 must update namespace imports, namespace-based filters, qualified constant references, and calls to the static `VisitCommands` overload. Existing service manager integrations should move to `SetFactory` or a service provider before 3.0.
