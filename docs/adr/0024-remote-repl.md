# 0024. Remote REPL over a Named Pipe

Status: Accepted
Date: 2026-11-04

## Context

An administration console for a running application needs a way to receive commands from another process. The application process opens a named pipe. A client, which is another process or the same executable started in client mode, connects to it, sends lines, and receives the output, the errors and the exit code of each line.

## Decision

Add `ReplPipeServer(pipeName, createSession)` and `ReplPipeClient(pipeName)` in the `CyberInternauts.CmdStarter.Repl.Remote` namespace.

The transport is a .NET named pipe (`System.IO.Pipes`) with `PipeOptions.CurrentUserOnly`, so only the user who started the server can connect.

One session runs at a time, on a single pipe instance. Each session has its own `ReplStarter`, created by the application's factory, and therefore its own state: a `login` is valid only for that session. The server disposes nothing that the factory creates. The factory must create a new `ReplStarter` for each session: under dependency injection, it builds one per call and does not resolve a singleton `ReplStarter`, which would share a login between sessions and let a remote `exit` stop the local REPL. A factory that returns the instance of the previous session, or a `ReplStarter` whose local `Launch` loop is running, makes `RunAsync` throw `InvalidOperationException`, as a configuration error.

Each line received is a REPL line: built-in words, sequences and pipes included ([ADR 0011](0011-repl-mode.md), [ADR 0022](0022-command-sequences.md), [ADR 0023](0023-text-pipes.md)). The server runs it through the same method as the loop of `ReplStarter.Launch`. The session receives a `DefaultConsole` whose input is empty and whose output and error output go to the pipe ([ADR 0021](0021-command-console.md)).

`RunAsync(cancellationToken)` accepts sessions one after the other until cancellation. A session ends on `exit` or `quit`, or when the client disconnects. An input or output error on the pipe (a client that disconnects in the middle of an exchange) closes that session only. Cancellation closes the current session and ends `RunAsync` without an exception. A configuration error (for example, a factory that returns an empty namespace filter) is not swallowed: it leaves `RunAsync` and stops the server. An exception thrown by the session factory leaves `RunAsync` the same way.

The server never touches `System.Console`, its streams or Ctrl+C. The local REPL of the process console remains usable while a remote session is open.

The client's `ConnectAsync(timeout)` throws `TimeoutException` when no free server accepts the session before the timeout, either because a session is already open or because of the moment between two sessions. `ExecuteAsync(line, output, error)` sends the line, copies the output and the errors without their prefix, and returns the exit code. A blank line is not sent and returns 0. A line that contains a line break is refused. `RunInteractiveAsync(input, output, error, prompt)` loops until its input ends, until the server closes the session, or after an `exit` or `quit` line. The client does not know the exit words of the server, and assumes these two.

## Protocol

The protocol is UTF-8 text without BOM, one line per message, each line ended by `\n`.

- Client to server: one command line. A blank line does nothing and gets no response.
- Server to client, for each other line: lines `O <text>` (output) and `E <text>` (error), in the order in which they were written, then `X <code>`, which ends the response. `<code>` is the exit code of the line, in the sense of a sequence.

A multi-line output gives several `O` lines. An empty output line gives `O `. The rest of a line that was not terminated is sent before the `X`.

For `exit` or `quit`, the server sends `X 0` and then closes the session.

## Security

There is no network access, only the current user (`CurrentUserOnly`). The server itself does not enforce per-command rights. A filter ([ADR 0019](0019-command-filters.md)) can refuse an execution and is the place for such checks. Command states ([ADR 0012](0012-command-states.md)) only decide which commands are offered: they are not a security measure.

## Exclusions

The following are outside this decision:

- several simultaneous sessions, because the state is per session and a `Starter` is not designed for concurrent `Start` calls;
- TCP or any other network access;
- authentication;
- daemon mode (passing `software.exe <args>` to a server that is already running). This is left for a later decision, based on the startup measurements ([performance](../advanced/performance.md)).

## Consequences

An application exposes its own administration console without any extra dependency, with the same commands, filters and states as its local REPL. Commands must write to the `CommandConsole` they receive for their output to reach the client.
