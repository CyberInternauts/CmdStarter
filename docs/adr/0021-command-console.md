# 0021. Command Console

Status: Accepted
Date: 2026-11-04

## Context

Command handlers wrote to `Console.Out`, which belongs to the process. A remote REPL, a text pipe between commands, or an in-memory test needs each execution to have its own input, output and error output. System.CommandLine already writes help and parse errors to `InvocationConfiguration.Output` and `Error`, and CmdStarter writes its own messages there too (an exception message, or a command unavailable in the current state).

## Decision

Add `CommandConsole`, a public sealed class in the `CyberInternauts.CmdStarter` namespace. Its constructor `CommandConsole(TextReader input, TextWriter output, TextWriter error)` rejects `null`, and it exposes the `In`, `Out` and `Error` properties. `CommandConsole.Standard` returns, on each read, a console over `System.Console.In`, `System.Console.Out` and `System.Console.Error` as they are at that moment.

`Starter.DefaultConsole` (a `CommandConsole?`, `null` by default, meaning the process console) is the console of every execution started without an explicit console.

`Starter` gets the new overloads `Start(string[] args, CommandConsole console, CancellationToken cancellationToken = default)` and `Start(string args, CommandConsole console, CancellationToken cancellationToken = default)`. `ReplStarter` has them too, where they also raise `OnCommandExecuted`. The REPL loop runs each line with `DefaultConsole`.

A handler parameter of type `CommandConsole` receives the console of the execution, in both models (classes and methods). Like a `CancellationToken`, it does not become an argument.

`CommandContext.Console` gives the same console to filters ([ADR 0019](0019-command-filters.md)).

With a console, its output and error writers become the `Output` and `Error` of the invocation configuration. Help, parse errors and CmdStarter messages therefore go to that console.

## Where the Console Lives

The console is attached to the invocation configuration of the execution itself, in a `ConditionalWeakTable<InvocationConfiguration, CommandConsole>` owned by the `Starter`. The `ParseResult` of a handler leads back to it. There is no field and no ambient state. A field would be overwritten by a nested `Start` on the same `Starter` (a handler that calls `Start` would lose its console on return), and by two simultaneous executions. An `AsyncLocal` would depend on the flow of the `ExecutionContext` and would need restoring. Here, a nested `Start` and simultaneous executions each keep their own console by construction. The table is weak: the console disappears with the invocation.

The condition is that `CreateInvocationConfiguration` returns a new instance on each call. The base class returns `null`, and `ReplStarter` creates a new instance.

A `Starter` is still not intended for concurrent `Start` calls, because it stores the parsed global options; only the console is per execution.

## Without a Console

Without an explicit console and without `DefaultConsole`, the call is exactly what it was before, and the handler receives `CommandConsole.Standard`, read at execution time. A `Console.SetOut` done after the `Starter` is created is therefore still honored, as before.

## What Handlers Must Do

CmdStarter does not redirect `System.Console`. A handler that wants to follow the console of its execution writes to the `CommandConsole` it receives, not to `Console`. The existing examples keep `Console.Out`. The container does not inject `CommandConsole` into constructors, and the prompt of `ConsoleReplProvider` stays on the process console.

## Consequences

One application can serve the process console, a remote session or a test with `StringWriter`s, without changing its commands, provided that they write to the `CommandConsole` they receive.
