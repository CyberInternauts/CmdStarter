// Sample: an administration console served over a named pipe, one session at a time.

using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Repl;
using CyberInternauts.CmdStarter.Repl.Remote;
using CyberInternauts.CmdStarter.Samples.ReplServer.Commands;

if (args is ["serve", var servePipe])
{
    return await ServeAsync(servePipe);
}

if (args is ["connect", var connectPipe])
{
    return await ConnectAsync(connectPipe);
}

if (args is ["demo"])
{
    return await DemoAsync();
}

Console.Error.WriteLine("Usage: serve <pipe> | connect <pipe> | demo");
return 2;

// Each session gets its own ReplStarter, so its state ends with the session.
static ReplStarter CreateSession() =>
    new(new ConsoleReplProvider()) // The server never reads this provider, but a ReplStarter needs one.
    {
        Namespaces = [typeof(AdminCommands).Namespace!]
    };

static async Task<int> ServeAsync(string pipeName)
{
    using var cancellation = new CancellationTokenSource();
    ConsoleCancelEventHandler stop = (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellation.Cancel();
    };
    Console.CancelKeyPress += stop;
    try
    {
        Console.Out.WriteLine($"Serving on pipe '{pipeName}'. Press Ctrl+C to stop.");
        await new ReplPipeServer(pipeName, CreateSession).RunAsync(cancellation.Token);
        return 0;
    }
    finally
    {
        Console.CancelKeyPress -= stop;
    }
}

static async Task<int> ConnectAsync(string pipeName)
{
    await using var client = new ReplPipeClient(pipeName);
    try
    {
        await client.ConnectAsync(TimeSpan.FromSeconds(5));
    }
    catch (TimeoutException)
    {
        Console.Error.WriteLine($"No server accepted a session on pipe '{pipeName}'.");
        return 1;
    }

    return await client.RunInteractiveAsync(Console.In, Console.Out, Console.Error);
}

// A server and a client in the same process: run two commands, show each output and code, stop the server.
static async Task<int> DemoAsync()
{
    var pipeName = "cmdstarter-sample-" + Guid.NewGuid().ToString("N");
    using var cancellation = new CancellationTokenSource();
    var server = new ReplPipeServer(pipeName, CreateSession).RunAsync(cancellation.Token);

    await using (var client = new ReplPipeClient(pipeName))
    {
        await client.ConnectAsync(TimeSpan.FromSeconds(10));
        foreach (var line in new[] { "status", "echo hi" })
        {
            var code = await client.ExecuteAsync(line, Console.Out, Console.Error);
            Console.Out.WriteLine($"{line} -> {code}");
        }
    }

    await cancellation.CancelAsync();
    await server.WaitAsync(TimeSpan.FromSeconds(10));
    return 0;
}
