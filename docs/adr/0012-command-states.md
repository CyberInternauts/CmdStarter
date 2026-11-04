# 0012. Command States

Status: Accepted
Date: 2026-11-03

## Context

Interactive command applications need help and executable commands to follow the application's current state. Examples include logged-in and logged-out sessions and the steps of a workflow. The command tree should remain stable while the application changes state between REPL inputs.

## Decision

CmdStarter defines no application states. The application supplies its own state values, commonly an enum, and an `ICommandStateProvider` whose `CurrentState` reports the current value. `Starter.StateProvider` takes precedence; otherwise CmdStarter resolves `ICommandStateProvider` from `Starter.ServiceProvider`. Applications using dependency injection should register the provider as a singleton so state changes persist across command scopes.

`[AvailableInState(...)]` can be applied to a class-based command, a method-based command group, or a method-based command. The state value is read once before each `Start` execution, after command instantiation. The command is available when its own attribute is absent or one of its listed values equals the current value according to `object.Equals`, and every parent command is available. An unavailable command makes its complete subtree unavailable.

When at least one command has `[AvailableInState]`, a missing provider is a configuration error: `InstantiateCommands` throws `InvalidAttributeException`. For each execution, CmdStarter updates command availability and sets `Command.Hidden` when the command has `[Hidden]` or is unavailable. The command tree is not rebuilt. A command unavailable in the current state is refused before its handler instance or execution scope is created; CmdStarter writes `Command '<path>' is not available in the current state.` to the invocation error writer, returns exit code `1`, and does not raise `CommandHandled`.

A single class command rooted on the root command remains visible in help because its symbols are placed on the root command. Its handler is still refused when its state does not allow execution.

## Consequences

Applications can change their current state without rebuilding the command tree, and help follows that state on the next execution. Hiding a command is not a security measure; handlers for sensitive operations must check authorization themselves. Applications with multiple state dimensions can return a combined value, such as a flags enum or record. More expressive availability conditions can be added later without changing this decision.
