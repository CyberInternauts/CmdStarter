# 0019. Command Filters

Status: Accepted
Date: 2026-11-04

## Context

Applications often need to run the same work around many command handlers: logging, timing, confirmation, authorization checks, or translating exceptions. A completion event cannot wrap an execution, stop it before the handler, or replace its exit code.

## Decision

Add `ICommandFilter`, whose `InvokeAsync(CommandContext context, Func<Task<int>> next)` method receives the bound execution context and the next step. Calling `next` invokes the remaining filters and then the command. A filter can omit that call to prevent execution, and the integer it returns becomes the command's exit code.

`CommandContext` exposes the System.CommandLine `Command`, its path, the `MemberInfo` that defines the command (the command type for class commands and the attributed method for method commands), the bound instance, `ParseResult`, the execution scope's `IServiceProvider` when configured, and the cancellation token.

`Starter.AddFilter(ICommandFilter)` registers an instance that CmdStarter does not dispose. `Starter.AddFilter<TFilter>()`, `CmdStarterOptions.AddFilter<TFilter>()`, and `CmdStarterBuilder.AddFilter<TFilter>()` register a filter type. Type filters resolve from the current execution scope when registered in dependency injection; otherwise CmdStarter creates them with `ActivatorUtilities` and the scoped services, or with a parameterless constructor when there is no service provider. CmdStarter disposes instances it creates after each execution, including when a filter throws. Filters run in registration order, with the first registered filter on the outside. Adding a filter does not rebuild the command tree.

Filter types added through the options or builder are registered as transient services. This lets their constructors use scoped dependencies and lets `ValidateOnBuild` check their dependency graphs.

## Execution Placement

The command is first checked for availability. If it is unavailable in the current state, CmdStarter returns exit code `1` before activating a command instance or running any filter.

For an available command, CmdStarter creates the command activation and binds global options, class-command options, or method parameter values. It then creates the `CommandContext` and invokes the filter chain around the command handler. The innermost step invokes the handler, converts cancellation with the canceled invocation token to exit code `130`, and raises `CommandHandled` with that handler exit code. A short-circuiting filter therefore prevents both the handler and `CommandHandled`. The code returned from the outermost filter is the final command exit code.

Exceptions from filters follow the same command exception handling as exceptions from handlers: by default, CmdStarter writes the exception message on one line and returns `1`; `ShowExceptionDetails` delegates full details to System.CommandLine. A canceled `OperationCanceledException` returns `130`.

## Relationship to Command States

ADR 0012 defines that a state can make a command unavailable and that CmdStarter refuses that command before invoking its handler. Filters run after this availability check. A state can hide a command from the current command tree; a filter can refuse execution of an available command after its inputs have been bound. Applications should continue to enforce sensitive authorization at the appropriate layer.

## Consequences

Applications can compose cross-cutting behavior once and use the bound command metadata, values, services, and cancellation token. Filters can alter the result or prevent a handler from running. Filter failures use the existing exception and exit-code behavior, and each execution-scoped filter has a clear release point.
