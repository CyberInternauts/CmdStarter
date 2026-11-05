# Errors and exit codes

This guide lists the exit codes of a CmdStarter application, what decides each one, and how configuration errors are reported.

## Exit codes

| Situation | Exit code | Decided by |
| --- | --- | --- |
| The command finishes without a result, or its handler returns `void`, `Task` or `ValueTask` | 0 | CmdStarter |
| The handler returns an `int`, `Task<int>` or `ValueTask<int>` | the returned value | the handler |
| Command-line error: unknown command, missing argument, value that cannot be read, failed validation | non-zero | System.CommandLine |
| Exception thrown during activation, option binding, the handler or the release of the command | 1 | CmdStarter |
| Command not available in the current state | 1 | CmdStarter |
| Cancelled command: its token is cancelled and its handler throws `OperationCanceledException` | 130 | CmdStarter |
| Sequence or pipe | the code of its last command that ran | CmdStarter |
| Remote REPL answer | the code after `X` in the answer line | `ReplPipeServer` |

A command-line error returns a non-zero code. The exact value comes from System.CommandLine, not from CmdStarter, so check that the code is non-zero, not a specific value. `Start` does not throw for a command-line error.

## The handler's own code

A handler chooses the code it returns. In this example, `Transfer` returns 2 when the amount is not positive:

<!-- snippet: samples/CmdStarter.Samples.Testing/Commands/TransferCommands.cs -->
```csharp
public static int Transfer(string from, string to, IAccounts accounts, [Option] int amount = 0)
{
    if (amount <= 0)
    {
        Console.Error.WriteLine("The amount must be positive.");
        return 2;
    }
```

```powershell
dotnet run --project samples/CmdStarter.Samples.Testing -- transfer alice bob --amount 0
$LASTEXITCODE
```

## Command-line errors

A command-line error is a parse error, and `Start` returns a non-zero code. A failed validation is a parse error too, as the [Validation](validation.md) guide describes. The Validation sample rejects a port out of range:

```powershell
dotnet run --project samples/CmdStarter.Samples.Validation -- serve --port 70000
```

## Unhandled exceptions

If activation, option binding, the handler or the release of the command instance throws, CmdStarter writes only the exception's message, on one line, to the error output, and returns 1.

In this example, the method `Fail` throws an `InvalidOperationException`:

<!-- snippet: CmdStarter.Tests.App/Methods/UserCommands.cs -->
```csharp
[Command]
public static void Fail() => throw new InvalidOperationException("integration failure");
```

Set `Starter.ShowExceptionDetails` to `true` to hand the same exceptions to System.CommandLine. It then prints the whole exception, with its type and its message, and the code is still 1. In this example, the test application sets the property from an environment variable:

<!-- snippet: CmdStarter.Tests.App/Program.cs -->
```csharp
var builder = new CmdStarterBuilder().AddCommandsFromAssembly(typeof(Program).Assembly);
builder.ConfigureStarter(starter => starter.ShowExceptionDetails = Environment.GetEnvironmentVariable("CMDSTARTER_TESTAPP_DETAILS") == "1");
```

## Command not available

A command that its state makes unavailable is refused. CmdStarter writes `Command '<path>' is not available in the current state.` to the error output and returns 1. See [Command states](command-states.md).

## Cancelled commands

A command returns 130 when its cancellation token is cancelled and its handler throws `OperationCanceledException`. The token is the one given to `Start`. In a REPL, it is the token of the running command, which Ctrl+C or `CancelCurrentCommand()` cancels. In this example, the method waits for its token:

<!-- snippet: CmdStarter.Tests.Commands/Cancellation/CancellableMethods.cs -->
```csharp
[Command]
public static async Task<int> WaitForCancel(CancellationToken token)
{
    Started.TrySetResult();
    await Task.Delay(Timeout.Infinite, token);
    return 0;
}
```

## Sequences and pipes

A sequence returns the code of the last command it ran. `&&` skips the next command after a failure, `||` runs it only after a failure, and `;` always runs it. A command that returns 130 stops the sequence: in `a; b`, if `a` returns 130, `b` does not run and the sequence returns 130. A pipe returns the code of its last command, and it runs every command, even after a failure. See [Sequences and pipes](sequences-and-pipes.md).

A line with no operator runs as a plain command, even when it has an unclosed quote. In the REPL, a line with an operator that cannot be split, such as `hello &&`, and a pipeline that contains `exit`, such as `hello | exit` (`Command 'exit' cannot be piped.`), return 1, give the reason on the error output, run nothing, and leave the session open.

In a remote session, the code of each line comes back as the answer line `X <code>`, so these lines are answered with `X 1`. A blank line has no answer. See [Remote REPL](remote-repl.md).

## Configuration errors

Configuration errors are exceptions, not exit codes. They are thrown while the command tree is built, and they are not turned into an exit code. The three public exceptions derived from `CmdStarterException` are:

- `InvalidAttributeException`: an invalid attribute or definition, or a name or alias conflict;
- `LoopException`: a parent loop between command types;
- `NoCommandFoundException`: a filter left no command. Its `LastFilterApplied` property names the filter: `Filter.Namespaces`, `Filter.Classes` or `Filter.Assemblies`.

Other configuration errors are not `CmdStarterException`s. For example, a command factory that returns `null` throws `InvalidOperationException`, and `Starter.UseTree` throws `ArgumentException` or `InvalidOperationException` (see [Fluent tree](fluent-tree.md)). So do not expect a `catch (CmdStarterException)` to catch every configuration error.

## Name conflicts

When the tree is built, CmdStarter checks that:

- a command and its direct subcommands do not share a name or an alias;
- the options of one command, `--help` and `--version` included, do not share a name or an alias;
- with `Starter.SequenceOption` set, that option is neither a root option nor a first-level command.

In this example, the alias of `Beta` is `alpha`, the name of its sibling `Alpha`:

<!-- snippet: CmdStarter.Tests.Commands/Erroneous/NameConflicts/AliasVsName.cs -->
```csharp
public sealed class Alpha : StarterCommand
{
    // ...
[Alias("alpha")]
public sealed class Beta : StarterCommand
```

`InvalidAttributeException` reports the conflict when the tree is built.

What is not checked at run time: an option on a subcommand that masks a global option inherited from the root, as [ADR 0010](../adr/0010-errors-and-exit-codes.md) describes. The analyzer reports it at compile time (`CMDS0012`, see [Analyzers](../advanced/analyzers.md)).

## See also

- Examples: [Testing](../../samples/CmdStarter.Samples.Testing), [Validation](../../samples/CmdStarter.Samples.Validation), [DevOpsTool](../../samples/CmdStarter.Samples.DevOpsTool).
- Neighbouring pages: [Validation](validation.md), [Command states](command-states.md), [REPL](repl.md), [Sequences and pipes](sequences-and-pipes.md), [Remote REPL](remote-repl.md), [Attributes](../concepts/attributes.md).
- ADR: [0007, parse errors](../adr/0007-parse-errors.md), [0010, errors and exit codes](../adr/0010-errors-and-exit-codes.md), [0022, command sequences](../adr/0022-command-sequences.md), [0023, text pipes](../adr/0023-text-pipes.md), [0024, remote REPL](../adr/0024-remote-repl.md).
