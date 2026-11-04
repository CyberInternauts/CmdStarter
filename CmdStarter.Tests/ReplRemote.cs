using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Repl;
using CyberInternauts.CmdStarter.Repl.Remote;
using CyberInternauts.CmdStarter.Tests.Commands.Remote;
using CyberInternauts.CmdStarter.Tests.Commands.Repl;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("Remote")]
public class ReplRemote
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);

    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [Test]
    public async Task ClientReceivesOutputAndExitCode()
    {
        await using var server = new RunningServer();
        await using var client = await server.ConnectAsync();
        var (code, output, error) = await ExecuteAsync(client, "hello");

        Assert.That(code, Is.EqualTo(0));
        Assert.That(output, Is.EqualTo("hello\n"));
        Assert.That(error, Is.Empty);
    }

    [Test]
    public async Task ErrorsAreSeparatedFromOutput()
    {
        await using var server = new RunningServer();
        await using var client = await server.ConnectAsync();
        var (code, output, error) = await ExecuteAsync(client, "oops");

        Assert.That(code, Is.EqualTo(2));
        Assert.That(error, Is.EqualTo("oops\n"));
        Assert.That(output, Is.Empty);
    }

    [Test]
    public async Task SessionKeepsStateBetweenLines()
    {
        await using var server = new RunningServer();
        await using var client = await server.ConnectAsync();

        Assert.That((await ExecuteAsync(client, "whoami")).Output, Is.EqualTo("anonymous\n"));
        Assert.That((await ExecuteAsync(client, "login")).Code, Is.EqualTo(0));
        Assert.That((await ExecuteAsync(client, "whoami")).Output, Is.EqualTo("admin\n"));
    }

    [Test]
    public async Task NextSessionStartsFresh()
    {
        await using var server = new RunningServer();
        await using (var first = await server.ConnectAsync())
        {
            Assert.That((await ExecuteAsync(first, "login")).Code, Is.EqualTo(0));
        }

        await using var second = await server.ConnectAsync();
        Assert.That((await ExecuteAsync(second, "whoami")).Output, Is.EqualTo("anonymous\n"));
    }

    [Test]
    public async Task ExitEndsSessionAndServerAcceptsNextClient()
    {
        await using var server = new RunningServer();
        await using var first = await server.ConnectAsync();

        Assert.That((await ExecuteAsync(first, "exit")).Code, Is.EqualTo(0));

        await using var second = await server.ConnectAsync();
        Assert.That((await ExecuteAsync(second, "hello")).Output, Is.EqualTo("hello\n"));
    }

    [Test]
    public async Task SequencesAndPipesWorkRemotely()
    {
        await using var server = new RunningServer();
        await using var client = await server.ConnectAsync();

        var (code, output, error) = await ExecuteAsync(client, "hello; oops || hello");
        Assert.That(code, Is.EqualTo(0));
        Assert.That(output, Is.EqualTo("hello\nhello\n"));
        Assert.That(error, Is.EqualTo("oops\n"));

        // A pipeline: the first command output is the input of the last one, which ignores it and writes its own.
        var piped = await ExecuteAsync(client, "hello | hello");
        Assert.That(piped.Code, Is.EqualTo(0));
        Assert.That(piped.Output, Is.EqualTo("hello\n"));
    }

    [Test]
    public async Task OnlyOneSessionAtATime()
    {
        await using var server = new RunningServer();
        await using var first = await server.ConnectAsync();
        await using var second = new ReplPipeClient(server.PipeName);

        Assert.ThrowsAsync<TimeoutException>(async () => await second.ConnectAsync(TimeSpan.FromMilliseconds(500)));
    }

    [Test]
    public async Task CancellationStopsServer()
    {
        // Without any client.
        var idle = new RunningServer();
        await idle.StopAsync();
        await idle.DisposeAsync();

        // With a connected client.
        var busy = new RunningServer();
        await using var client = await busy.ConnectAsync();
        Assert.That((await ExecuteAsync(client, "hello")).Code, Is.EqualTo(0));
        await busy.StopAsync();
        await busy.DisposeAsync();
    }

    [Test]
    public async Task LocalReplRunsWhileServing()
    {
        await using var server = new RunningServer();
        await using var client = await server.ConnectAsync();
        Assert.That((await ExecuteAsync(client, "hello")).Code, Is.EqualTo(0));

        // A local REPL, distinct from the sessions of the server, keeps its own console while a session is open.
        using var localOutput = new StringWriter();
        using var localError = new StringWriter();
        var local = CreateSession("hello");
        local.DefaultConsole = new CommandConsole(TextReader.Null, localOutput, localError);
        await local.Launch().WaitAsync(Limit);

        Assert.That(localOutput.ToString(), Does.Contain("hello"));
        Assert.That((await ExecuteAsync(client, "hello")).Output, Is.EqualTo("hello\n"));
    }

    private static ReplStarter CreateSession(params string?[] lines) => new(new ScriptedInputProvider(lines))
    {
        // Namespaces and the service provider are set before the session is used: method parameters are classified when the tree is built.
        Namespaces = ImmutableList.Create(typeof(Hello).Namespace!),
        ServiceProvider = new ServiceCollection().AddSingleton<SessionUser>().BuildServiceProvider()
    };

    private static async Task<(int Code, string Output, string Error)> ExecuteAsync(ReplPipeClient client, string line)
    {
        using var output = new StringWriter { NewLine = "\n" };
        using var error = new StringWriter { NewLine = "\n" };
        var code = await client.ExecuteAsync(line, output, error).WaitAsync(Limit);
        return (code, output.ToString(), error.ToString());
    }

    // A server running in the background on a pipe name of its own; disposing stops it and waits for its end.
    private sealed class RunningServer : IAsyncDisposable
    {
        private readonly CancellationTokenSource cancellation = new();
        private readonly Task run;

        internal RunningServer()
        {
            PipeName = "cmdstarter-test-" + Guid.NewGuid().ToString("N");
            run = new ReplPipeServer(PipeName, () => CreateSession()).RunAsync(cancellation.Token);
        }

        internal string PipeName { get; }

        internal async Task<ReplPipeClient> ConnectAsync()
        {
            var client = new ReplPipeClient(PipeName);
            await client.ConnectAsync(Limit);
            return client;
        }

        internal async Task StopAsync()
        {
            await cancellation.CancelAsync();
            await run.WaitAsync(Limit);
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            cancellation.Dispose();
        }
    }
}
