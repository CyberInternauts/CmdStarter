# Daemon mode

In daemon mode, an application that is already running executes the command line of the next invocation. `app <args>` hands its arguments to the running application through a named pipe, and runs them itself only when no running application answers. This guide covers the server, the session of a daemon, the forwarding, the fallback, and what is not forwarded. The Daemon sample shows all of it.

## Why

Starting a new process costs the startup of the application. [Performance](../advanced/performance.md), after the activation cache, measures 137.3 ms for the first instantiation in a new process, and 4.55 µs to execute a command in a process that already runs. A running application can therefore execute `app <args>` much sooner than a new process, and it keeps its state (a connection, a cache) from one invocation to the next. The page measures the two parts separately: it gives no figure for a forwarded call.

## The server

The daemon is a `ReplPipeServer`, as described in [Remote REPL](remote-repl.md), that the application starts itself, for example in a `serve` mode. Nothing is new on the server side: one session runs at a time, and each invocation that is forwarded is one session.

A pipe name is shared by all the users of the machine. The sample adds the name of the user to its fixed name, so that a daemon of another user does not own the pipe:

<!-- snippet: samples/CmdStarter.Samples.Daemon/Program.cs -->
```csharp
// A pipe name is shared by all the users of the machine: the name of the user keeps this daemon apart from the one of another user.
var pipeName = "cmdstarter-sample-daemon-" + Environment.UserName;
```

The state that a daemon keeps lives in the services of its process. The sample builds them once and gives them to every session, so the `Counter` that the `count` command increments survives the sessions:

<!-- snippet: samples/CmdStarter.Samples.Daemon/Commands/CounterCommands.cs -->
```csharp
[Command]
public static int Count(CommandConsole console, Counter counter)
{
    counter.Value++;
    console.Out.WriteLine(counter.Value);
    return 0;
}
```

## The session of a daemon

A forwarded invocation is a line of REPL for the session. The first word of the line therefore keeps the meaning it has in a REPL: the words in `ReplStarter.ExitCommands` end the session, and the words in `ReplStarter.HelpCommands` show the help of the command that follows.

`app exit` against a default session ends the session and answers 0, where `Start` on the same arguments is a parse error. To get the behaviour of `Start` for the arguments of a process, the session factory of a daemon empties both lists:

<!-- snippet: samples/CmdStarter.Samples.Daemon/Program.cs -->
```csharp
static ReplStarter CreateSession(IServiceProvider services) =>
    new(new ConsoleReplProvider()) // The server never reads this provider, but a ReplStarter needs one.
    {
        Namespaces = [typeof(CounterCommands).Namespace!],
        ServiceProvider = services,
        // A forwarded invocation then runs as Start(args) would: "app exit" and "app help" are not words of the session.
        ExitCommands = [],
        HelpCommands = []
    };
```

With both lists empty, `exit` and `help` are treated as any other word: in the sample, which has no command of these names, a parse error with the same exit code that a local `Start` gives.

## Forward the arguments

`ReplPipeClient.TryForwardAsync` connects to the server, runs the arguments, copies the output and the errors to a `CommandConsole`, and returns the exit code. It returns `null` when the application has to run the arguments itself. The sample forwards its arguments and falls back to a local `Starter`:

<!-- snippet: samples/CmdStarter.Samples.Daemon/Program.cs -->
```csharp
return await ReplPipeClient.TryForwardAsync(pipeName, args, TimeSpan.FromMilliseconds(500), CommandConsole.Standard, local.SequenceOption) ?? await local.Start(args);
```

`CommandConsole.Standard` is the console of the process. The local `Starter` of the sample has the same `Namespaces` as the session, and its own services.

### How the arguments become a line

`ReplPipeClient.JoinArguments` writes the line, and `TryForwardAsync` uses the same rules. The arguments are joined by one space, and an argument is surrounded by double quotes when:

- it is empty;
- one of its characters is white space for `char.IsWhiteSpace`, which includes the tab and the no-break space, not only the space;
- it contains `;`, `&` or `|`, so that an argument such as `a&&b` is never read as an operator.

Any other argument is written as it is. A quoted `;` is an argument and not an operator, so the arguments of a process never form a sequence or a pipe.

An argument that contains a double quote, a carriage return or a line feed cannot be written on a line, because neither the reading of sequences nor the splitting of the line has an escape. `JoinArguments` throws `ArgumentException` for it, and `TryForwardAsync` returns `null`, so that the application runs the arguments itself.

### A sequence given as one argument

When `sequenceOption` is not `null` and the arguments are exactly that option followed by one line, `TryForwardAsync` sends the line as it is, because a line of REPL is already a sequence. This is the same rule as `Starter.Start` with `Starter.SequenceOption` ([Sequences and pipes](sequences-and-pipes.md)): with `-c`, the arguments `-c "hello; oops"` run `hello` and then `oops` on the server. Such a line may contain double quotes. A line that is blank, or that contains a line break, gives `null`. Without `sequenceOption`, the same arguments are written as arguments: the line `-c "hello; oops"` reaches the session, where `-c` is a parse error.

## The fallback

`TryForwardAsync` waits for a server that accepts the session up to its timeout, and returns `null` when none does. The client does not tell a daemon that is absent from one that is busy with another session: in both cases the answer is the timeout and then `null`.

Without any server, the connection is retried until the timeout, so every local fallback costs the whole timeout. Choose a short timeout. The sample uses 500 ms.

`TryForwardAsync` also returns `null`, without opening anything, when the list of arguments is empty (`app` alone runs locally, for its help or its REPL), when an argument cannot be written on a line, and when a sequence line cannot be sent.

## What is not forwarded

- The standard input. A remote command reads no standard input: its input is empty.
- The writes on `System.Console`. A command that writes there instead of on its `CommandConsole` writes on the console of the daemon.

## Errors

Once the session is open, the command may have run on the server. A failure from then on is not turned into `null`, because the application must not run the command a second time. These exceptions leave `TryForwardAsync`:

- `IOException`, when the session closed before the answer ended;
- `InvalidDataException`, when the server sent a line that the protocol does not know;
- `OperationCanceledException`, when the token is cancelled.

An invalid argument of the call is a mistake of the caller: a pipe name that is null, empty or blank, `arguments` or `console` that is null, and an argument that is null.

The pipe keeps `PipeOptions.CurrentUserOnly`: only the user who runs the daemon can send it a line. See [Remote REPL](remote-repl.md).

## Run the sample

The `demo` mode runs the daemon and its callers in one process. It forwards `count` twice, stops the daemon, and then runs `count` locally on services of their own, as another process would:

```powershell
dotnet run --project samples/CmdStarter.Samples.Daemon -- demo
```

It writes `forwarded: 1`, `forwarded: 2` and `local: 1`. To try it with two terminals, run the daemon in one, then any command in the other:

```powershell
dotnet run --project samples/CmdStarter.Samples.Daemon -- serve
dotnet run --project samples/CmdStarter.Samples.Daemon -- count
```

## See also

- Examples: [Daemon](../../samples/CmdStarter.Samples.Daemon/Program.cs).
- Neighbouring pages: [Remote REPL](remote-repl.md), [Performance](../advanced/performance.md), [Sequences and pipes](sequences-and-pipes.md).
- ADR: [0026, daemon mode](../adr/0026-daemon-mode.md).
