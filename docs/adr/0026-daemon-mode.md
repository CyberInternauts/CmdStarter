# 0026. Daemon Mode

Status: Accepted
Date: 2026-11-04

## Context

Starting an application costs its startup: the first instantiation in a new process takes 137.3 ms, while executing a command in a process that already runs takes 4.55 µs (measures after the activation cache, [performance](../advanced/performance.md)). An application that is already running can therefore execute `app <args>` much sooner than a new process, and it can keep a state (a connection, a cache) between two invocations.

[ADR 0024](0024-remote-repl.md) put a REPL on a named pipe and left daemon mode, which passes `app <args>` to a server that is already running, for a later decision based on these measurements. This is that decision.

## Decision

Add forwarding on the client side only. Nothing changes in `ReplPipeServer`.

- **The server is a `ReplPipeServer` as it is.** The application starts it itself, for example in a `serve` mode like the ReplServer sample. One session runs at a time, and each invocation that is forwarded is one session.
- **The client sends the arguments of the process as a line of REPL.** Each argument is quoted when it needs it, and the arguments are joined by one space. The line goes through the two readings that exist: `CommandSequence.TryParse`, where double quotes open and close a zone without any escape, and then `Start(string)`, whose splitting removes the quotes and has no escape either. An argument that contains a double quote, a carriage return or a line feed cannot be written on a line.
- **Local fallback.** When no server accepts the session before the timeout, the application runs the command itself. The client does not tell an absent server from a busy one, since one session runs at a time: in both cases the answer is the timeout and then the fallback.
- **No forwarding without arguments.** `app` alone runs locally, for its help or for the REPL of the application. A blank line has no answer from the server anyway.
- **Unchanged.** The standard input is not forwarded ([ADR 0024](0024-remote-repl.md): a remote command reads no standard input). A command that writes on `System.Console` instead of on its `CommandConsole` writes on the console of the server. The security stays `PipeOptions.CurrentUserOnly`.

Details of the API, `ReplPipeClient.JoinArguments` and `ReplPipeClient.TryForwardAsync`:

- **The sequence option.** A line of REPL is read by `Start(string)`, which never reads `Starter.SequenceOption` (only the overloads with an array of arguments do). `TryForwardAsync` takes the option as a parameter: when it is not `null` and the arguments are exactly that option followed by one line, the line is sent as it is, since a line of REPL is already a sequence. Such a line may contain double quotes. A blank line, or one that contains a line break, gives `null`. This is the same rule as `Starter.Start` with `Starter.SequenceOption`.
- **Built-in words.** A forwarded invocation is a line of REPL, so its first word keeps the meaning of the REPL: `app exit` ends the session and answers 0, `app help x` shows the help of `x`, and a command of the application named `help` or `exit` is hidden. `ReplStarter.ExitCommands` and `ReplStarter.HelpCommands` can be changed, and an empty list disables the words. A daemon whose invocations must behave as `Start(args)` empties both lists in its session factory.
- **Quoting.** The splitting of System.CommandLine cuts the line on every character for which `char.IsWhiteSpace` is true, and the session trims the line the same way. An argument is therefore quoted when it is empty, when one of its characters is white space for `char.IsWhiteSpace`, or when it contains `;`, `&` or `|`. A quoted empty argument reaches the command as an empty value, without any error.
- **Null elements.** A null element in the arguments is a mistake of the caller: `JoinArguments` and `TryForwardAsync` throw `ArgumentException`. This is distinct from an argument that cannot be written on a line, which gives `ArgumentException` from `JoinArguments` and `null` from `TryForwardAsync`.
- **Pipe name.** A pipe name is shared by all the users of the machine. The sample adds the name of the user to its fixed name. When a pipe of that name belongs to another user, `UnauthorizedAccessException` leaves `TryForwardAsync`.
- **Errors.** Once the session is open, the command may have run on the server. `IOException`, `InvalidDataException`, `OperationCanceledException` and `UnauthorizedAccessException` leave `TryForwardAsync` as they are, and the application does not run the command a second time. Only the timeout of the connection gives `null`.

`ReplPipeClient.ExecuteAsync` also takes a list of arguments, written as `JoinArguments` does, for a caller that already holds a connection.

## Exclusions

The following are outside this decision:

- the detection of an absent server, as distinct from a busy one;
- the launch of the server by the client when none answers;
- the forwarding of the standard input;
- several simultaneous sessions;
- network access.

## Consequences

An application can answer `app <args>` from a process that already runs, with a few lines in its entry point, and keep a state between invocations.

Without any server, the connection is retried until the timeout, so every local fallback costs the whole timeout. The timeout should be short (the sample uses 500 ms).

Parity with `Start(args)` requires a session without built-in words. With the default session, `app exit` ends the session instead of being a parse error.

The cost of a forwarded call is not measured: the figures above are those of a new process and of an execution in a running process.
