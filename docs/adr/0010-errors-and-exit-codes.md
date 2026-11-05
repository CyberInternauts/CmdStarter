# 0010. Errors and Exit Codes

Status: Accepted
Date: 2026-11-03

## Context
Applications need a consistent way to distinguish successful execution, handler results, invalid command-line input, handler exceptions, cancellation, and invalid CmdStarter configuration.

## Decision
Successful commands return exit code `0`. An integer result returned by a handler becomes the process exit code, including results from `Task<int>` and `ValueTask<int>` handlers.

System.CommandLine reports command-line parsing errors and returns a non-zero exit code, as described in [ADR 0007](0007-parse-errors.md). By default, an unhandled exception from command activation, binding, handling, or release writes only its message on one line to the invocation error output and returns exit code `1`. Set `Starter.ShowExceptionDetails` to `true` to use System.CommandLine's default exception handler, which writes the full exception and returns exit code `1`.

When a handler throws `OperationCanceledException` while its command cancellation token is cancelled, CmdStarter returns exit code `130` and does not emit an unhandled exception trace. The command handled event is raised with that exit code. Class-based command handlers can receive the invocation `CancellationToken` as a parameter; it does not create a command-line argument.

Configuration errors are exceptions derived from `CmdStarterException`. They are thrown while the command tree is built and are not converted to an exit code by CmdStarter.

## Name conflicts

Conflicting command names or aliases between a command and its direct subcommands, and conflicting option names or aliases on the same command, are configuration errors. `Starter.InstantiateCommands` throws `InvalidAttributeException` when it finds one. This catches conflicts before parsing: System.CommandLine otherwise throws a less informative `ArgumentException` only when parsing reaches the affected command, or silently leaves one of two conflicting options masked. An option on a subcommand that masks a global option inherited from the root is not checked.

## Consequences
Callers can use the process exit code for successful results, parse failures, command exceptions, and cancelled commands. Exception messages are concise by default; applications can enable full details through `ShowExceptionDetails`. Invalid application configuration is surfaced as a `CmdStarterException` during startup rather than as a command result.
