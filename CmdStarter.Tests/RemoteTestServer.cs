using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Repl;
using CyberInternauts.CmdStarter.Repl.Remote;
using CyberInternauts.CmdStarter.Tests.Commands.Remote;
using CyberInternauts.CmdStarter.Tests.Commands.Repl;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter.Tests;

// What the tests that open real named pipes share: the time limit, the sessions of the server and the running server itself.
internal static class RemoteTesting
{
    internal static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);

    internal static ReplStarter CreateSession(params string?[] lines) => new(new ScriptedInputProvider(lines))
    {
        // Namespaces and the service provider are set before the session is used: method parameters are classified when the tree is built.
        Namespaces = ImmutableList.Create(typeof(Hello).Namespace!),
        ServiceProvider = new ServiceCollection().AddSingleton<SessionUser>().BuildServiceProvider()
    };
}

// A server running in the background on a pipe name of its own; disposing stops it and waits for its end.
internal sealed class RunningServer : IAsyncDisposable
{
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task run;

    internal RunningServer(Func<ReplStarter>? createSession = null)
    {
        PipeName = "cmdstarter-test-" + Guid.NewGuid().ToString("N");
        run = new ReplPipeServer(PipeName, createSession ?? (() => RemoteTesting.CreateSession())).RunAsync(cancellation.Token);
    }

    internal string PipeName { get; }

    internal Task Run => run;

    internal async Task<ReplPipeClient> ConnectAsync()
    {
        var client = new ReplPipeClient(PipeName);
        await client.ConnectAsync(RemoteTesting.Limit);
        return client;
    }

    internal async Task StopAsync()
    {
        await cancellation.CancelAsync();
        await run.WaitAsync(RemoteTesting.Limit);
    }

    public async ValueTask DisposeAsync()
    {
        // A server that already failed (a test that expects it) has nothing left to stop.
        if (!run.IsFaulted)
        {
            await StopAsync();
        }

        cancellation.Dispose();
    }
}
