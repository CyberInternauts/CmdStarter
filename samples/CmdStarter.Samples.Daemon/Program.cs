// Sample: a daemon. "serve" keeps the application running; any other call forwards its arguments to it, or runs them itself when no daemon answers.

using System.Collections.Immutable;
using CyberInternauts.CmdStarter;
using CyberInternauts.CmdStarter.Repl;
using CyberInternauts.CmdStarter.Repl.Remote;
using CyberInternauts.CmdStarter.Samples.Daemon.Commands;
using Microsoft.Extensions.DependencyInjection;

// A pipe name is shared by all the users of the machine: the name of the user keeps this daemon apart from the one of another user.
var pipeName = "cmdstarter-sample-daemon-" + Environment.UserName;

if (args is ["serve"])
{
    return await ServeAsync(pipeName);
}

if (args is ["demo"])
{
    return await DemoAsync();
}

// The daemon runs the arguments when it answers within 500 ms; otherwise this process runs them itself.
var local = CreateLocalStarter(CreateServices());
return await ReplPipeClient.TryForwardAsync(pipeName, args, TimeSpan.FromMilliseconds(500), CommandConsole.Standard, local.SequenceOption) ?? await local.Start(args);

static ServiceProvider CreateServices() => new ServiceCollection().AddSingleton<Counter>().BuildServiceProvider();

static Starter CreateLocalStarter(IServiceProvider services) =>
    new()
    {
        Namespaces = ImmutableList.Create(typeof(CounterCommands).Namespace!),
        ServiceProvider = services
    };

// Each forwarded invocation is a session with its own ReplStarter; the services are built once per process, so the counter survives the sessions.
static ReplStarter CreateSession(IServiceProvider services) =>
    new(new ConsoleReplProvider()) // The server never reads this provider, but a ReplStarter needs one.
    {
        Namespaces = ImmutableList.Create(typeof(CounterCommands).Namespace!),
        ServiceProvider = services,
        // A forwarded invocation then runs as Start(args) would: "app exit" and "app help" are not words of the session.
        ExitCommands = ImmutableList<string>.Empty,
        HelpCommands = ImmutableList<string>.Empty
    };

static async Task<int> ServeAsync(string pipeName)
{
    var services = CreateServices();
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
        await new ReplPipeServer(pipeName, () => CreateSession(services)).RunAsync(cancellation.Token);
        return 0;
    }
    finally
    {
        Console.CancelKeyPress -= stop;
    }
}

// The daemon and its callers in one process: two forwarded counts, then the daemon stops and the third count runs locally.
static async Task<int> DemoAsync()
{
    var pipeName = "cmdstarter-sample-daemon-" + Guid.NewGuid().ToString("N");
    var daemonServices = CreateServices();
    using var cancellation = new CancellationTokenSource();
    var server = new ReplPipeServer(pipeName, () => CreateSession(daemonServices)).RunAsync(cancellation.Token);

    foreach (var _ in new[] { 1, 2 })
    {
        using var output = new StringWriter();
        var code = await ReplPipeClient.TryForwardAsync(pipeName, ["count"], TimeSpan.FromSeconds(10), new CommandConsole(TextReader.Null, output, Console.Error));
        if (code is not 0)
        {
            Console.Error.WriteLine("The daemon did not run the command.");
            return 1;
        }

        Console.Out.WriteLine($"forwarded: {output.ToString().Trim()}");
    }

    await cancellation.CancelAsync();
    await server.WaitAsync(TimeSpan.FromSeconds(10));

    // No daemon any more: the arguments come back to run here, on services of their own, as another process would have.
    if (await ReplPipeClient.TryForwardAsync(pipeName, ["count"], TimeSpan.FromMilliseconds(200), CommandConsole.Standard) is not null)
    {
        Console.Error.WriteLine("A daemon answered, but it was stopped.");
        return 1;
    }

    using var localOutput = new StringWriter();
    var localCode = await CreateLocalStarter(CreateServices()).Start(["count"], new CommandConsole(TextReader.Null, localOutput, Console.Error));
    Console.Out.WriteLine($"local: {localOutput.ToString().Trim()}");
    return localCode;
}
