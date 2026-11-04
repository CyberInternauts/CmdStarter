# 0010. Errors and Exit Codes

Status: Accepted
Date: 2026-11-03

## Context
Applications need a consistent way to distinguish successful execution, handler results, invalid command-line input, handler exceptions, cancellation, and invalid CmdStarter configuration.

## Decision
Successful commands return exit code `0`. An integer result returned by a handler becomes the process exit code, including results from `Task<int>` and `ValueTask<int>` handlers.

System.CommandLine reports command-line parsing errors and returns a non-zero exit code, as described in [ADR 0007](0007-parse-errors.md). An unhandled handler exception is written to standard error by System.CommandLine and returns exit code `1` with System.CommandLine 2.0.14.

When a handler throws `OperationCanceledException` while its command cancellation token is cancelled, CmdStarter returns exit code `130` and does not emit an unhandled exception trace. The command handled event is raised with that exit code. Class-based command handlers can receive the invocation `CancellationToken` as a parameter; it does not create a command-line argument.

Configuration errors are exceptions derived from `CmdStarterException`. They are thrown while the command tree is built and are not converted to an exit code by CmdStarter.

## Consequences
Callers can use the process exit code for successful results, parse failures, and cancelled commands. Handler exceptions remain visible on standard error. Invalid application configuration is surfaced as a `CmdStarterException` during startup rather than as a command result.
