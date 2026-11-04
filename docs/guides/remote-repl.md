# Remote REPL

A remote REPL serves a REPL over a named pipe on the local machine, so that another process can send it command lines. This guide covers the server, the client, the session factory, and the protocol they speak. The ReplServer sample shows both ends.

## Sessions

`ReplPipeServer` serves a REPL over a named pipe on the local machine, one session at a time. `ReplPipeClient` connects to it. Each session has its own `ReplStarter`, so a login, or any other state, lives and ends with the session. Built-in words, sequences and pipes work as they do in a local REPL.

The pipe allows one server instance. A second client waits for the session in progress to end, and `ConnectAsync` throws `TimeoutException` if no server is free before its timeout.

## Start a server

Create the server with the pipe name and a function that creates a session, then call `RunAsync` with a cancellation token. `RunAsync` accepts sessions one after another until the token is cancelled, and it ends quietly on cancellation. The ReplServer sample cancels the token on Ctrl+C:

<!-- snippet: samples/CmdStarter.Samples.ReplServer/Program.cs -->
```csharp
await new ReplPipeServer(pipeName, CreateSession).RunAsync(cancellation.Token);
```

## The session factory

The factory must create a new `ReplStarter` for each session. Returning the instance of a previous session, or a `ReplStarter` whose own `Launch` loop is running, makes `RunAsync` throw `InvalidOperationException`, with the message that the factory must create a new `ReplStarter` for each session. The client that was connected then sees an `IOException`. A shared instance would carry one session's state into the next, and a remote `exit` would stop the local REPL. An `ObjectDisposedException` thrown by the factory itself, for example by a container that is being disposed, also leaves `RunAsync`, and the connected client sees an `IOException`.

The sample builds each session by hand, and sets the `Namespaces` of its `ReplStarter`:

<!-- snippet: samples/CmdStarter.Samples.ReplServer/Program.cs -->
```csharp
static ReplStarter CreateSession() =>
    new(new ConsoleReplProvider()) // The server never reads this provider, but a ReplStarter needs one.
    {
        Namespaces = ImmutableList.Create(typeof(AdminCommands).Namespace!)
    };
```

## Connect a client

`ReplPipeClient` is created with the pipe name. `ConnectAsync(TimeSpan timeout)` waits for a free server up to the timeout. `ExecuteAsync(line, output, error)` sends one line, copies the output and error lines that the server sends back, and returns the exit code of the line:

<!-- snippet: samples/CmdStarter.Samples.ReplServer/Program.cs -->
```csharp
await using var client = new ReplPipeClient(pipeName);
await client.ConnectAsync(TimeSpan.FromSeconds(5));
var code = await client.ExecuteAsync(line, Console.Out, Console.Error);
```

`RunInteractiveAsync(input, output, error, prompt)` runs a loop around the same call: it writes a prompt, reads a line from `input`, runs it on the server, and starts again. The default prompt is `> `. The sample's `connect` mode returns this loop's code:

<!-- snippet: samples/CmdStarter.Samples.ReplServer/Program.cs -->
```csharp
return await client.RunInteractiveAsync(Console.In, Console.Out, Console.Error);
```

The loop ends at the end of the input, when the server closes the session, or after `exit` or `quit`, which the client recognizes itself; it returns the code of the last line it ran.

## Errors

A line that the server refuses is answered with code 1, and the session goes on. A malformed sequence, or a piped `exit`, is refused this way: the reason is written to the error output, nothing else runs, and the next line works. Sequences and pipes are described in [Sequences and pipes](sequences-and-pipes.md).

## Protocol

The text is UTF-8 without a byte-order mark, one message per line, each line ended by `\n`. The client sends one line of the REPL. The server answers with `O ` followed by the text for each output line and `E ` followed by the text for each error line, in the order they were written, then `X ` followed by the exit code, which ends the answer. A blank line gets no answer.

A remote command reads no standard input: its input is empty.

## Access

The pipe is opened with `PipeOptions.CurrentUserOnly` on the local machine, so only the user who runs the server can connect. What a connected user may do is for the application to decide, for example with filters (see [Filters](filters.md)).

## Run the sample

The `demo` mode runs a server and a client in the same process: it runs `status` and then `echo hi`.

```powershell
dotnet run --project samples/CmdStarter.Samples.ReplServer -- demo
```

To try a server and a client in two terminals, use the `serve` and `connect` modes with the same pipe name:

```powershell
dotnet run --project samples/CmdStarter.Samples.ReplServer -- serve <pipe>
dotnet run --project samples/CmdStarter.Samples.ReplServer -- connect <pipe>
```

The commands of the sample are `Status` and `Echo`, and `Echo` writes its text to the console it receives:

<!-- snippet: samples/CmdStarter.Samples.ReplServer/Commands/AdminCommands.cs -->
```csharp
[Command]
public static int Echo(CommandConsole console, string text)
{
    console.Out.WriteLine(text);
    return 0;
}
```

## See also

- Examples: [ReplServer](../../samples/CmdStarter.Samples.ReplServer/Program.cs).
- Neighbouring pages: [REPL](repl.md), [Sequences and pipes](sequences-and-pipes.md), [Command console](command-console.md), [Filters](filters.md).
- ADR: [0024, remote REPL](../adr/0024-remote-repl.md).
