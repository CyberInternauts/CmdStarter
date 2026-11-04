# 0011. REPL Mode

Status: Accepted
Date: 2026-11-03

## Context

Interactive command applications need a predictable input loop, a way to leave it, help for the current command tree, cancellation for long-running commands, and the same dependency injection support as non-interactive applications.

## Decision

`ReplStarter` reads lines through `IReplInputProvider.GetInputAsync`. A `null` line ends the loop, and empty or whitespace-only lines are ignored. `ExitCommands` defaults to `exit` and `quit`; `HelpCommands` defaults to `help`. Both compare words without case sensitivity, take priority over application commands with the same names, and can be disabled with an empty list. `help` runs root help, while `help <command line>` runs help for that command line. `ConsoleReplProvider` writes its configurable prompt (default `> `) before reading each line.

A cancellation token passed to `Launch` can stop input and cancel the active command. Ctrl+C while a command is running cancels that command's token; Ctrl+C while waiting for input, or a second Ctrl+C during the same command, keeps the console's normal process-termination behavior. A cancelled command whose handler throws `OperationCanceledException` while its token is cancelled returns exit code `130`. `CancelCurrentCommand` provides the same active-command cancellation programmatically.

`CmdStarterBuilder.UseRepl` configures a `ReplStarter`, and `CmdStarterApp.RunReplAsync` starts it. Each command runs in its own DI scope. Singleton services remain shared across commands. The built application registers its `Starter` as a service; in REPL mode the same instance is also registered as `ReplStarter`.

## Consequences

REPL input providers must support asynchronous, cancellable reads and signal end of input with `null`. Applications can use built-in commands and cancellation without adding command types. Dependency lifetimes match regular command execution.
