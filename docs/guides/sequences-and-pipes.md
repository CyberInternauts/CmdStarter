# Sequences and pipes

A line can hold several commands joined by operators, and a single `|` passes the text output of one command to the next. This guide covers both, the exit codes they return, and their use in the REPL.

## Operators

Three operators join commands:

- `;` always runs the next command.
- `&&` runs the next command only if the last executed command returned 0.
- `||` runs the next command only if the last executed command returned something other than 0.

The line is read from left to right. There is no precedence and no grouping.

A skipped command leaves the last code as it was. In `build && deploy`, when `build` returns 3, `deploy` does not run and the line returns 3. In `build || notify`, `notify` runs only after a failure.

## Quotes

Operators count only outside double quotes. A `;` between double quotes is part of the argument, so `greet "Ann; Bob"` is one command. A single `&` is plain text, so `a & b` is one command. Empty steps between `;` are skipped: `a;;b;` is `a` then `b`, and a blank line has no step.

## Exit codes and errors

A command that returns 130 stops the sequence. The whole line then returns 130, and nothing after it runs, whatever the operator. Cancellation with Ctrl+C is described in [Errors and exit codes](errors-and-exit-codes.md).

A malformed line returns 1, writes the reason to the error output, and runs nothing. For example, a line that starts with `&&`, ends with `&&`, or has an unclosed double quote is malformed. The reason messages are written in English and are not translated.

A line with no operator runs exactly as a plain `Start` does, even when it is not a valid sequence. An unclosed double quote on such a line gives the System.CommandLine behaviour, not the sequence message.

## Run a sequence outside the REPL

`Starter.StartSequence(line, token)` runs a sequence, and `StartSequence(line, console, token)` runs it with a console. It returns the code of the last executed command, or 1 for a malformed line.

To let the command line carry a whole sequence, set `Starter.SequenceOption` to an option name, for example `-c`. When it is set, `Start` called with exactly two arguments, the first of which is that option, runs the second argument as a sequence. An application that sets `SequenceOption` to `-c` then accepts `app -c "login ann; whoami"` on its command line. The option is off (`null`) by default.

Without the option set, `-c` is an unknown option. The option is refused when the command tree is built if it has the same name as a root option or a first-level command, with `InvalidAttributeException`.

Because `;`, `&&`, `||` and `|` are also operators of the shell, pass the whole sequence as one quoted argument, as in `app -c "a; b | c"`.

## Pipes

A single `|` pipes the text output of a command into the input of the next command. Each command except the last runs with its output captured in memory. The captured text becomes the `In` of the next command. The first command reads the input of the execution's console, and the last command writes to the output of the execution's console. A command reads its input from `CommandConsole.In`, as described in [Command console](command-console.md).

For example, a command that writes lines to `console.Out` can be the first command of a pipe, and a command that reads `console.In` can be the second:

<!-- snippet: CmdStarter.Tests.Commands/Sequences/SequenceCommands.cs -->
```csharp
public sealed class Emit : StarterCommand
{
    public override Delegate HandlingMethod => (Func<CommandConsole, int>)(console =>
    {
        console.Out.WriteLine("b");
        console.Out.WriteLine("a");
        console.Out.WriteLine("c");
        return 0;
    });
}
```

A pipe has these rules:

- Errors are not piped. Each command's error output goes straight to the error output of the execution's console.
- A pipe returns the code of its last command. Unlike `&&`, a command that fails does not stop the pipe: every command runs.
- A pipe is one step of a sequence. In `build | count && deploy`, `deploy` runs only if `count` returns 0.
- A step without `|` runs its command as it would without a pipe: nothing is captured.
- A malformed pipe, such as a `|` with no command before or after it, is a malformed line, as described above. `||` is the operator for alternatives and is never read as two pipes.

## In the REPL

In the REPL, every typed line is a sequence. Each command of the line raises `ReplStarter.OnCommandExecuted`, as described in [REPL](repl.md). An `exit` inside a sequence ends the REPL, and the commands after it do not run.

`exit` and `quit` cannot be piped. In a pipe of several commands, the REPL refuses the whole pipe with `Command 'exit' cannot be piped.`, returns 1, and goes on reading lines.

Completion in the REPL works on the step under the cursor. After a `;`, `&&`, `||` or `|`, the commands are proposed for the new step; `exit` is proposed at the start of a step, as a built-in word. After `help` in a step, commands are proposed, but not `exit`. With the cursor inside an operator, nothing from the next step is proposed.

## See also

- Examples: [Repl](../../samples/CmdStarter.Samples.Repl/Program.cs).
- Neighbouring pages: [REPL](repl.md), [Command console](command-console.md), [Errors and exit codes](errors-and-exit-codes.md), [Remote REPL](remote-repl.md) (sequences and pipes work remotely too), [Command states](command-states.md).
- ADR: [0022, command sequences](../adr/0022-command-sequences.md), [0023, text pipes](../adr/0023-text-pipes.md).
