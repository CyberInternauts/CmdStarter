# 0022. Command Sequences

Status: Accepted
Date: 2026-11-04

## Context

Typing `login; list` or `build && deploy` in the REPL, or passing the same line in one invocation (`app -c "login; list"`), should run the commands in order while the application state stays in memory from one command to the next. A shell interprets `;`, `&&`, `||` and `|` before the application sees them. For that reason the normal form passes the whole line in one quoted option value, rather than as a raw command line.

## Semantics

Commands run from left to right. After `;`, the next command always runs. After `&&`, it runs only if the last executed exit code is 0. After `||`, it runs only if that code is not 0. A skipped command does not change the last exit code. The exit code of a sequence is that of the last executed command. A cancelled command (exit code 130) stops the sequence.

Operators count only outside double quotes, the only quotes System.CommandLine recognizes (there is no escaping). The quotes remain in the text of the step. A single `|` and a single `&` remain plain text. Empty steps after `;` are ignored.

The following are refused, with a message that names the operator:

- a line that starts with `&&` or `||`;
- an `&&` or `||` followed by an empty step or by another operator;
- an `&&` or `||` directly after a `;`;
- an unclosed quote in a line that contains an operator.

A line without an operator runs exactly as before, even when it is not a valid sequence: an unclosed quote is left to System.CommandLine.

`CommandSequence.TryParse` (namespace `CyberInternauts.CmdStarter.Sequences`) returns the steps. Each step is a `CommandSequenceStep(CommandLine, Operator)`, and `SequenceOperator` has the values `None`, `Always`, `OnSuccess` and `OnFailure`.

## In the REPL

The `ReplStarter` loop treats each line as a sequence. The built-in words keep their meaning in each step: `ok; exit; other` runs `ok` and then ends the loop, and `help status && ok` shows the help of `status` (a success) and then runs `ok`. `OnCommandExecuted` is raised for each executed command. An invalid line writes its message to the error output (`DefaultConsole`, otherwise the process console), and the loop continues. See [ADR 0011](0011-repl-mode.md).

## StartSequence and SequenceOption

`Starter.StartSequence(commandLine, cancellationToken)` and `Starter.StartSequence(commandLine, console, cancellationToken)` run a sequence. Each step goes through the matching `Start` overload. When a console is given, it serves every step ([ADR 0021](0021-command-console.md)). An invalid line writes its message to the error output of the console (the explicit one, otherwise `DefaultConsole`, otherwise the process console) and returns 1. `ReplStarter` has its own `StartSequence` methods, which raise `OnCommandExecuted` for each step.

`Starter.SequenceOption` is `null` by default, so nothing changes. When it has a value such as `-c`, `Start(args)` with exactly two arguments whose first one is that option runs the second one as a sequence. Building the commands throws `InvalidAttributeException` when this value is also the name or an alias of an option of the root (help, version and global options included) or of a top-level command. Changing `SequenceOption` rebuilds the tree, so that this check always sees the current value.

## Completion

In the REPL, `Tab` completes the step that contains the cursor. For example, `ok; ot` proposes `other`. This also works on an unfinished line (`ok && ` or an open quote), and built-in words are proposed in the first word of a step. When the cursor is in the middle of an operator, the whole line is completed. The current word of the editor starts after the last white space, `;`, `&` or `|`, so `ok;ot` completes without a space. The cost is that an `[AutoComplete]` value containing one of these characters is not completed beyond that character. See [ADR 0020](0020-repl-line-editing.md).

## Consequences

Sequences work in the REPL and in a single invocation without changing the commands. A line without an operator behaves as before. Pipes, parentheses, variables and single quotes are outside this decision.
