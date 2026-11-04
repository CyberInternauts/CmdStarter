# REPL

A REPL reads command lines in a loop until it stops; this guide covers the loop, the built-in words, cancellation, dependency injection and line editing.

## The loop

`ReplStarter` is a `Starter` that reads command lines from an `IReplInputProvider` in a loop. `ReplStarter.Launch()` starts the loop. `Launch` with a command, given as a string array or as a string, runs that command first and then starts the loop. The overloads that take a `CancellationToken` accept a token that ends the loop.

`IReplInputProvider.GetInputAsync(CancellationToken)` returns the next line, or `null` at the end of the input, and `null` ends the loop. Blank lines and lines with only white space are skipped.

A line can also be a sequence or a pipe. See [Sequences and pipes](sequences-and-pipes.md).

`ConsoleReplProvider` reads from the console. Its `Prompt` (default `"> "`) is written before each line. It can be set only when the object is created.

## Built-in words

The built-in words `exit` and `quit` end the loop, whatever their case. `help` shows the application's help, and `help <command>` shows that command's help.

`ExitCommands` and `HelpCommands` are lists of words that you can replace. An empty list disables the built-in words, and the word is then sent to the command parser, where it fails. In this example, `ExitCommands` is emptied, so `exit` is no longer a built-in word:

<!-- snippet: CmdStarter.Tests/Repl.cs -->
```csharp
var codes = new List<int>();
var repl = CreateScriptedStarter(codes, "exit");
repl.ExitCommands = [];
```

## Stopping the loop

After each command, `ReplStarter.OnCommandExecuted` is raised with the exit code, in `ReplCommandEventArgs.ReturnCode`. It is raised for every command that runs through this `ReplStarter`, whichever `Start` overload or reference type calls it, nested calls included.

`ReplStarter.Stop()` ends the loop from code: called from an `OnCommandExecuted` handler, it ends the loop after the current command, before the next line is read.

## Cancelling a command

Ctrl+C, or `ReplStarter.CancelCurrentCommand()` from code, cancels only the running command, not the REPL, as [ADR 0011](../adr/0011-repl-mode.md) describes. The command returns 130, and the REPL reads the next line. `CancelCurrentCommand()` returns `true` when it cancelled a command, and `false` when no command was running.

For example, while a command waits:

<!-- snippet: CmdStarter.Tests/Repl.cs -->
```csharp
Assert.That(repl.CancelCurrentCommand(), Is.True);
await run.WaitAsync(TimeSpan.FromSeconds(10));
Assert.That(codes, Is.EqualTo(new[] { 130 }));
```

## With dependency injection

`CmdStarterBuilder.UseRepl(provider)` makes the application a REPL, and `CmdStarterApp.RunReplAsync()` runs it. Every command runs in its own service scope, and singletons live as long as the application. The `ReplStarter` is registered as a service, and it is the same object as the `Starter`. Calling `RunReplAsync` without `UseRepl` throws `InvalidOperationException`.

<!-- snippet: samples/CmdStarter.Samples.Repl/Program.cs -->
```csharp
var builder = new CmdStarterBuilder()
    .AddCommandsFromAssembly(typeof(Program).Assembly)
    .UseRepl(new ConsoleReplProvider());
builder.Services.AddSingleton<Session>();
builder.Services.AddScoped<CommandScope>();

await using var app = builder.Build();
await app.RunReplAsync();
```

In the Repl sample, `Session` is a singleton that keeps the user between lines. Each command gets its own `CommandScope`, which counts the commands:

<!-- snippet: samples/CmdStarter.Samples.Repl/Commands/SessionCommands.cs -->
```csharp
[Command]
public static void Login(string user, Session session, CommandScope scope)
{
    session.User = user;
    Console.WriteLine($"Logged in as {user} (scope {scope.Number})");
}
```

<!-- snippet: samples/CmdStarter.Samples.Repl/Services/CommandScope.cs -->
```csharp
public sealed class CommandScope
{
    private static int created;

    public int Number { get; } = Interlocked.Increment(ref created);
}
```

## Line editing

`ConsoleReplProvider.LineEditing` (default `true`) turns on the line editor, with history and Tab completion. The editor is skipped when the standard input or the standard output is redirected: the line is then read as it is, after the prompt. `ConsoleReplProvider.HistorySize` (default 100) bounds the session history. Its value must be at least 1, or `ArgumentOutOfRangeException` is thrown.

`ReplLineEditor` handles these keys:

- Enter validates the line.
- Backspace and Delete remove a character.
- Left and Right, Home and End move the cursor.
- Up and Down walk the history. The unfinished line is kept as a draft and comes back.
- Tab completes, as described below.
- Escape clears the line.
- Ctrl+D or Ctrl+Z on an empty line signals the end of input, and the loop ends.

The history keeps the non-blank lines of the session, without an immediate repeat, and drops the oldest lines beyond `HistorySize`. `ReplLineEditor.History` lists it.

Tab completes the word before the cursor from `CompletionSource`. One candidate completes the word and adds a space. Several candidates complete their common prefix, and a second Tab lists them. The current word starts after the last white space, `;`, `&` or `|`, so `ok;ot` completes without a space.

`ReplStarter.GetCompletions(line, position)` gives the candidates for a cursor position: the visible commands of the current state, their options and their `[AutoComplete]` values, and the built-in words when the cursor is in the first word. After a help word, the rest of the line is completed as a command line. In a sequence, only the step under the cursor is completed. `ConsoleReplProvider` uses it automatically, unless a `CompletionSource` is already set. For example, on an empty line (`CommandOneName` holds the name of a command of the application):

<!-- snippet: CmdStarter.Tests/ReplLineEditing.cs -->
```csharp
var completions = CreateReplStarter().GetCompletions(string.Empty, 0);

Assert.That(completions, Does.Contain(CommandOneName));
Assert.That(completions, Does.Contain("exit"));
Assert.That(completions, Does.Contain("help"));
```

The editor does not support lines wider than the console.

The state of the application drives the help and the completion of the REPL. See [Command states](command-states.md).

## Run the sample

```powershell
dotnet run --project samples/CmdStarter.Samples.Repl
```

Then type `login ann`, `whoami` and `exit`. On one line, `login ann; whoami` runs both commands.

## See also

- Examples: [Repl](../../samples/CmdStarter.Samples.Repl), [CommandStates](../../samples/CmdStarter.Samples.CommandStates), [ReplServer](../../samples/CmdStarter.Samples.ReplServer).
- Neighbouring pages: [Command states](command-states.md), [Sequences and pipes](sequences-and-pipes.md), [Remote REPL](remote-repl.md), [Hosting](hosting.md), [Errors and exit codes](errors-and-exit-codes.md), [Dependency injection](../concepts/dependency-injection.md).
- ADR: [0011, REPL mode](../adr/0011-repl-mode.md), [0020, REPL line editing](../adr/0020-repl-line-editing.md), [0022, command sequences](../adr/0022-command-sequences.md), [0023, text pipes](../adr/0023-text-pipes.md), [0024, remote REPL](../adr/0024-remote-repl.md).
