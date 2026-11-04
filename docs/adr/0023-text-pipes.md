# 0023. Text Pipes

Status: Accepted
Date: 2026-11-04

## Context

Commands such as `list | sort | count` should pass the output of one command to the next one, in the REPL and in a sequence (`StartSequence`, the sequence option). The pipe goes through `CommandConsole` ([ADR 0021](0021-command-console.md)): a command writes to `console.Out`, and the next one reads `console.In`.

## Decision

Inside a sequence step, a single `|` outside double quotes separates the commands of a pipeline. `||` keeps its meaning of "otherwise" ([ADR 0022](0022-command-sequences.md)). `CommandSequenceStep.Pipeline` returns these commands, trimmed. A step without `|` has one command and runs exactly as before, without any console being imposed.

The following are refused, with a message that names `|`:

- a `|` at the start or at the end of a step;
- two `|` separated only by white space, which covers `a || | b` and `a | && b`.

A line that contains a `|` is a sequence. An unclosed double quote is refused in that line (`Unclosed double quote.`), whereas in a line without any operator it is left to System.CommandLine.

The pipeline is buffered, not streamed. Each command runs to completion and its output is kept in memory. The next command then starts with that output as its input.

The console of the execution (the explicit one, otherwise `DefaultConsole`, otherwise the process console) provides the input of the first command and receives the output of the last one. The error output of each command goes directly to the error output of the execution, never into the pipe.

The exit code of a pipeline is that of its last command, as in bash without `pipefail`. All commands run even when one fails, except that a cancellation (130) stops the pipeline and the sequence. Within a sequence, the operators see this exit code: `fail | count && ok` runs `ok` when `count` returns 0.

Each command goes through the `Start` overload that takes a console. Filters, command states and `OnCommandExecuted` apply to it, once per command in the REPL.

A handler that writes to `System.Console` instead of the `CommandConsole` it receives is not captured.

## Built-in Words

In the REPL, a `help` command can be part of a pipeline, and its help text goes into the pipe.

`exit` and `quit` are refused in a pipeline of more than one command, with the message `Command 'exit' cannot be piped.` (the word that was typed) on the error output. The exit code is 1, nothing in the pipeline runs, and the sequence continues with that code.

## Completion

In the REPL, `Tab` completes the command of the pipeline that contains the cursor, so `emit | so` proposes `sort`. See [ADR 0022](0022-command-sequences.md).

## Exclusions

The following are outside this decision: streamed pipes, object pipes, and redirections (`>`, `<`). A first command that reads the standard input of the process in normal mode has no special handling: it reads the `In` of the console of the execution.

## Consequences

Commands that read and write through their `CommandConsole` compose without change. All data stays in memory, which suits the output of an administration console, but not large volumes.
