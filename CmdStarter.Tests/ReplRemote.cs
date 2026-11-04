using System.Collections.Immutable;
using System.IO.Pipes;
using System.Text;
using CyberInternauts.CmdStarter.Repl;
using CyberInternauts.CmdStarter.Repl.Remote;
using CyberInternauts.CmdStarter.Tests.Commands.Remote;
using CyberInternauts.CmdStarter.Tests.Commands.Repl;
using static CyberInternauts.CmdStarter.Tests.RemoteTesting;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("Remote")]
public class ReplRemote
{
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
    public async Task InvalidLinesAnswerOne()
    {
        await using var server = new RunningServer();
        await using var client = await server.ConnectAsync();

        var invalid = await ExecuteAsync(client, "hello &&");
        Assert.That(invalid.Code, Is.EqualTo(1));
        Assert.That(invalid.Error, Is.Not.Empty);
        Assert.That(invalid.Output, Is.Empty);

        var piped = await ExecuteAsync(client, "hello | exit");
        Assert.That(piped.Code, Is.EqualTo(1));
        Assert.That(piped.Error, Does.Contain("cannot be piped"));
        Assert.That(piped.Output, Is.Empty);

        // The session goes on after both lines.
        Assert.That((await ExecuteAsync(client, "hello")).Code, Is.EqualTo(0));
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

    [Test]
    public async Task FactoryMustCreateNewSession()
    {
        // A factory that hands out the same starter (a singleton resolved from a container, for instance) would share one session's state with the next.
        var shared = CreateSession();
        var server = new RunningServer(() => shared);
        await using (var first = await server.ConnectAsync())
        {
            Assert.That((await ExecuteAsync(first, "hello")).Code, Is.EqualTo(0));
        }

        await using var second = await server.ConnectAsync();
        Assert.ThrowsAsync<IOException>(async () => await ExecuteAsync(second, "hello"));
        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await server.Run.WaitAsync(Limit));
        Assert.That(exception!.Message, Does.Contain("new ReplStarter for each session"));
        await server.DisposeAsync();
    }

    [Test]
    public async Task ExceptionOfTheSessionFactoryLeavesRunAsync()
    {
        // A factory that fails (here, a container that is being disposed) is a configuration error: it is not swallowed with the errors of the pipe.
        var server = new RunningServer(() => throw new ObjectDisposedException("container"));
        await using var client = await server.ConnectAsync();

        Assert.ThrowsAsync<IOException>(async () => await ExecuteAsync(client, "hello"));

        var exception = Assert.ThrowsAsync<ObjectDisposedException>(async () => await server.Run.WaitAsync(Limit));
        Assert.That(exception!.ObjectName, Is.EqualTo("container"));
        await server.DisposeAsync();
    }

    [Test]
    public async Task IOExceptionOfTheSessionFactoryLeavesRunAsync()
    {
        // The errors of the pipe close one session; an error of the factory is another matter, even when its type is IOException.
        var server = new RunningServer(() => throw new IOException("the factory could not reach its container"));
        await using var client = await server.ConnectAsync();

        Assert.ThrowsAsync<IOException>(async () => await ExecuteAsync(client, "hello"));

        var exception = Assert.ThrowsAsync<IOException>(async () => await server.Run.WaitAsync(Limit));
        Assert.That(exception!.Message, Is.EqualTo("the factory could not reach its container"));
        await server.DisposeAsync();
    }

    [Test]
    public async Task FactoryMustNotReturnRunningLocalRepl()
    {
        // The local REPL is running: serving it would let a remote exit stop it and would take over its console.
        using var localCancellation = new CancellationTokenSource();
        var input = new WaitingInputProvider();
        var local = new ReplStarter(input) { Namespaces = ImmutableList.Create(typeof(Hello).Namespace!) };
        var localLoop = local.Launch(localCancellation.Token);
        await input.Waiting.WaitAsync(Limit);

        var server = new RunningServer(() => local);
        await using var client = await server.ConnectAsync();
        Assert.ThrowsAsync<IOException>(async () => await ExecuteAsync(client, "hello"));
        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await server.Run.WaitAsync(Limit));
        Assert.That(exception!.Message, Does.Contain("new ReplStarter for each session"));
        await server.DisposeAsync();

        Assert.That(localLoop.IsCompleted, Is.False);
        await localCancellation.CancelAsync();
        await localLoop.WaitAsync(Limit);
    }

    [Test]
    public async Task RawProtocolUsesPrefixedLines()
    {
        await using var server = new RunningServer();
        await using var pipe = new NamedPipeClientStream(".", server.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await pipe.ConnectAsync((int)Limit.TotalMilliseconds);
        var encoding = new UTF8Encoding(false);
        using var reader = new StreamReader(pipe, encoding, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        // Not disposed: the server closes the pipe after "exit", and disposing a StreamWriter flushes the pipe, which then throws "Pipe is broken".
        // SendAsync flushes every line, so nothing is left in the writer; the pipe itself is disposed by its "await using".
        var writer = new StreamWriter(pipe, encoding, bufferSize: 1024, leaveOpen: true) { NewLine = "\n" };

        // Output lines are prefixed "O ", error lines "E ", and the answer ends with "X <code>".
        Assert.That(await SendAsync("hello"), Is.EqualTo(new[] { "O hello", "X 0" }));
        Assert.That(await SendAsync("oops"), Is.EqualTo(new[] { "E oops", "X 2" }));

        // A blank line has no answer: the first line read after it belongs to the next command.
        await writer.WriteLineAsync(string.Empty);
        Assert.That(await SendAsync("hello"), Is.EqualTo(new[] { "O hello", "X 0" }));

        // After exit the answer is "X 0" and the server closes the session.
        Assert.That(await SendAsync("exit"), Is.EqualTo(new[] { "X 0" }));
        Assert.That(await reader.ReadLineAsync().WaitAsync(Limit), Is.Null);

        async Task<List<string>> SendAsync(string line)
        {
            await writer.WriteLineAsync(line);
            await writer.FlushAsync();
            var answer = new List<string>();
            while (true)
            {
                var received = await reader.ReadLineAsync().WaitAsync(Limit) ?? throw new IOException("The session closed before the answer ended.");
                answer.Add(received);
                if (received.StartsWith("X ", StringComparison.Ordinal))
                {
                    return answer;
                }
            }
        }
    }

    [Test]
    public async Task InteractiveLoopStopsAtExit()
    {
        await using var server = new RunningServer();
        await using var client = await server.ConnectAsync();
        using var input = new StringReader("hello\nexit\nhello");
        using var output = new StringWriter { NewLine = "\n" };
        using var error = new StringWriter { NewLine = "\n" };

        var code = await client.RunInteractiveAsync(input, output, error).WaitAsync(Limit);

        Assert.That(code, Is.EqualTo(0));
        Assert.That(output.ToString(), Is.EqualTo("> hello\n> "));
        Assert.That(error.ToString(), Is.Empty);

        // The loop stopped after exit: the last line was never read, so never sent.
        Assert.That(input.ReadLine(), Is.EqualTo("hello"));
    }

    [Test]
    public async Task ConnectingTwiceIsRefused()
    {
        await using var server = new RunningServer();
        await using var client = await server.ConnectAsync();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await client.ConnectAsync(Limit));

        Assert.That(exception!.Message, Does.Contain("already connected"));
        Assert.That((await ExecuteAsync(client, "hello")).Output, Is.EqualTo("hello\n"), "the first session still works");
    }

    [Test]
    public async Task ExecutingBeforeConnectingIsRefused()
    {
        await using var client = new ReplPipeClient("cmdstarter-test-" + Guid.NewGuid().ToString("N"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await ExecuteAsync(client, "hello"));

        Assert.That(exception!.Message, Does.Contain("not connected"));
    }

    [Test]
    public async Task LineWithALineBreakIsRefusedAndNothingIsSent()
    {
        await using var server = new RunningServer();
        await using var client = await server.ConnectAsync();

        foreach (var line in new[] { "hello\nhello", "hello\rhello" })
        {
            var exception = Assert.ThrowsAsync<ArgumentException>(async () => await ExecuteAsync(client, line));
            Assert.That(exception!.ParamName, Is.EqualTo("line"));
        }

        // Nothing was sent: the answer to the next line is the answer of that line alone.
        Assert.That((await ExecuteAsync(client, "hello")).Output, Is.EqualTo("hello\n"));
    }

    [Test]
    public async Task BlankLineAnswersZeroAndIsNotSent()
    {
        await using var raw = new RawServer();
        await using var client = new ReplPipeClient(raw.PipeName);
        await raw.ConnectAsync(client);

        Assert.That((await ExecuteAsync(client, "   ")).Code, Is.EqualTo(0));

        // The first line the server receives is the one that follows the blank line.
        var execution = ExecuteAsync(client, "hello");
        Assert.That(await raw.ReadLineAsync(), Is.EqualTo("hello"));
        await raw.WriteLineAsync("X 4");
        Assert.That((await execution.WaitAsync(Limit)).Code, Is.EqualTo(4));
    }

    [Test]
    public async Task InfiniteTimeoutStillConnects()
    {
        await using var server = new RunningServer();
        using var cancellation = new CancellationTokenSource(Limit);
        await using var client = new ReplPipeClient(server.PipeName);

        await client.ConnectAsync(Timeout.InfiniteTimeSpan, cancellation.Token);

        Assert.That((await ExecuteAsync(client, "hello")).Code, Is.EqualTo(0));
    }

    [Test]
    public async Task UnexpectedLinesFromTheServerAreRejectedWithTheLine()
    {
        // A server that does not follow the protocol: a prefix nobody knows, then an exit code that is not a number.
        foreach (var unexpected in new[] { "Z what", "X notanumber" })
        {
            await using var raw = new RawServer();
            await using var client = new ReplPipeClient(raw.PipeName);
            await raw.ConnectAsync(client);

            var execution = ExecuteAsync(client, "hello");
            Assert.That(await raw.ReadLineAsync(), Is.EqualTo("hello"));
            await raw.WriteLineAsync(unexpected);

            var exception = Assert.ThrowsAsync<InvalidDataException>(async () => await execution.WaitAsync(Limit));
            Assert.That(exception!.Message, Does.Contain(unexpected));
        }
    }

    [Test]
    public async Task InteractiveLoopStopsAtTheEndOfTheInput()
    {
        await using var server = new RunningServer();
        await using var client = await server.ConnectAsync();
        using var input = new StringReader("hello");
        using var output = new StringWriter { NewLine = "\n" };
        using var error = new StringWriter { NewLine = "\n" };

        var code = await client.RunInteractiveAsync(input, output, error).WaitAsync(Limit);

        Assert.That(code, Is.EqualTo(0));
        Assert.That(output.ToString(), Is.EqualTo("> hello\n> "));
    }

    [Test]
    public async Task InteractiveLoopSkipsBlankLines()
    {
        await using var server = new RunningServer();
        await using var client = await server.ConnectAsync();
        using var input = new StringReader("\n   \nhello\n");
        using var output = new StringWriter { NewLine = "\n" };
        using var error = new StringWriter { NewLine = "\n" };

        var code = await client.RunInteractiveAsync(input, output, error).WaitAsync(Limit);

        Assert.That(code, Is.EqualTo(0));
        Assert.That(output.ToString(), Is.EqualTo("> > > hello\n> "), "one prompt per line read, and only the last line is run");
    }

    [Test]
    public async Task InteractiveLoopEndsWhenTheServerClosesTheSession()
    {
        await using var raw = new RawServer();
        await using var client = new ReplPipeClient(raw.PipeName);
        await raw.ConnectAsync(client);
        using var input = new StringReader("a\nb\nc");
        using var output = new StringWriter { NewLine = "\n" };
        using var error = new StringWriter { NewLine = "\n" };

        var run = client.RunInteractiveAsync(input, output, error);
        Assert.That(await raw.ReadLineAsync(), Is.EqualTo("a"));
        await raw.WriteLineAsync("O hi");
        await raw.WriteLineAsync("X 5");
        Assert.That(await raw.ReadLineAsync(), Is.EqualTo("b"));
        await raw.CloseAsync();

        // No exception: the loop ends and gives the code of the last line that was answered.
        Assert.That(await run.WaitAsync(Limit), Is.EqualTo(5));
        Assert.That(output.ToString(), Is.EqualTo("> hi\n> "));
        Assert.That(input.ReadLine(), Is.EqualTo("c"), "the line after the closing was never read");
    }

    [Test]
    public async Task CharactersAndUnfinishedLinesBecomeProtocolLines()
    {
        await using var server = new RunningServer();
        await using var client = await server.ConnectAsync();

        var (code, output, error) = await ExecuteAsync(client, "fragments");

        Assert.That(code, Is.EqualTo(0));
        // 'x' 'y' '\n' is one line, a second '\n' is an empty line, a null string writes nothing, CR LF ends a line without the CR, and an unfinished line ends when the answer does.
        Assert.That(output, Is.EqualTo("xy\n\ncrlf\ntail\n"));
        Assert.That(error, Is.EqualTo("etail\n"));
    }

    [Test]
    public async Task ConsoleWritersOfASessionHaveTheEncodingOfThePipe()
    {
        await using var server = new RunningServer();
        await using var client = await server.ConnectAsync();

        var (code, output, _) = await ExecuteAsync(client, "encoding-name");

        Assert.That(code, Is.EqualTo(0));
        Assert.That(output, Is.EqualTo("utf-8\n"));
    }

    private static async Task<(int Code, string Output, string Error)> ExecuteAsync(ReplPipeClient client, string line)
    {
        using var output = new StringWriter { NewLine = "\n" };
        using var error = new StringWriter { NewLine = "\n" };
        var code = await client.ExecuteAsync(line, output, error).WaitAsync(Limit);
        return (code, output.ToString(), error.ToString());
    }

    // A server that is not a ReplPipeServer: it reads and writes the lines of the protocol by hand, on a pipe name of its own.
    private sealed class RawServer : IAsyncDisposable
    {
        private readonly NamedPipeServerStream pipe;
        private StreamReader? reader;
        private StreamWriter? writer;

        internal RawServer()
        {
            PipeName = "cmdstarter-test-" + Guid.NewGuid().ToString("N");
            pipe = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        }

        internal string PipeName { get; }

        internal async Task ConnectAsync(ReplPipeClient client)
        {
            using var cancellation = new CancellationTokenSource(Limit);
            var waiting = pipe.WaitForConnectionAsync(cancellation.Token);
            await client.ConnectAsync(Limit);
            await waiting;
            var encoding = new UTF8Encoding(false);
            reader = new StreamReader(pipe, encoding, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
            writer = new StreamWriter(pipe, encoding, bufferSize: 1024, leaveOpen: true) { NewLine = "\n" };
        }

        internal async Task<string?> ReadLineAsync() => await reader!.ReadLineAsync().WaitAsync(Limit);

        internal async Task WriteLineAsync(string line)
        {
            await writer!.WriteLineAsync(line);
            await writer.FlushAsync();
        }

        // Closes the session from the server side, as a server that stops does.
        internal async Task CloseAsync() => await pipe.DisposeAsync();

        public async ValueTask DisposeAsync()
        {
            // The writer is not disposed: it would flush into a pipe that the client may have closed.
            reader?.Dispose();
            await pipe.DisposeAsync();
        }
    }

    // An input that never comes: the local REPL waits in its loop until it is cancelled.
    private sealed class WaitingInputProvider : IReplInputProvider
    {
        private readonly TaskCompletionSource waiting = new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal Task Waiting => waiting.Task;

        public async ValueTask<string?> GetInputAsync(CancellationToken cancellationToken)
        {
            waiting.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return null;
        }
    }
}
