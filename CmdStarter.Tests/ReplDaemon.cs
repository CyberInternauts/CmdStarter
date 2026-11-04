using System.Collections.Immutable;
using System.Diagnostics;
using CyberInternauts.CmdStarter.Repl;
using CyberInternauts.CmdStarter.Repl.Remote;
using static CyberInternauts.CmdStarter.Tests.RemoteTesting;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("Remote")]
public class ReplDaemon
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [Test]
    public void JoinArgumentsKeepsPlainWords()
    {
        Assert.That(ReplPipeClient.JoinArguments(["hello", "--name", "ann"]), Is.EqualTo("hello --name ann"));
    }

    [Test]
    public void JoinArgumentsQuotesWhiteSpaceAndOperators()
    {
        Assert.That(ReplPipeClient.JoinArguments(["echo", "a b"]), Is.EqualTo("echo \"a b\""));

        // One case per character: a tab, a no-break space (white space for char.IsWhiteSpace, so for the splitting of System.CommandLine), then the operators.
        foreach (var argument in new[] { "a\tb", "a\u00A0b", "a;b", "a&b", "a&&b", "a|b", "a||b", ";", "&", "|" })
        {
            Assert.That(ReplPipeClient.JoinArguments(["echo", argument]), Is.EqualTo($"echo \"{argument}\""), $"argument '{argument}'");
        }

        // A plain argument next to a quoted one stays as it is.
        Assert.That(ReplPipeClient.JoinArguments(["a-b", "c d", "e"]), Is.EqualTo("a-b \"c d\" e"));
    }

    [Test]
    public void JoinArgumentsQuotesAnEmptyArgument()
    {
        Assert.That(ReplPipeClient.JoinArguments(["echo", string.Empty]), Is.EqualTo("echo \"\""));
        Assert.That(ReplPipeClient.JoinArguments([]), Is.Empty);
    }

    [Test]
    public void JoinArgumentsRefusesWhatCannotBeWrittenOnALine()
    {
        foreach (var argument in new[] { "a\"b", "a\nb", "a\rb" })
        {
            var exception = Assert.Throws<ArgumentException>(() => ReplPipeClient.JoinArguments(["echo", argument]));
            Assert.That(exception!.ParamName, Is.EqualTo("arguments"));
        }

        Assert.Throws<ArgumentNullException>(() => ReplPipeClient.JoinArguments(null!));

        var nullElement = Assert.Throws<ArgumentException>(() => ReplPipeClient.JoinArguments(["echo", null!]));
        Assert.That(nullElement!.Message, Does.Contain("index 1"));

        // A null element is a mistake of the caller even when an earlier argument cannot be written: the null is not hidden behind the refusal.
        var hidden = Assert.Throws<ArgumentException>(() => ReplPipeClient.JoinArguments(["a\"b", null!]));
        Assert.That(hidden!.Message, Does.Contain("index 1"));
    }

    [Test]
    public async Task ArgumentsAreExecutedAsOneLine()
    {
        await using var server = new RunningServer();
        await using var client = await server.ConnectAsync();
        using var output = new StringWriter { NewLine = "\n" };
        using var error = new StringWriter { NewLine = "\n" };

        // Quoted on the line, so ';' is an argument and not an operator; the splitting of System.CommandLine removes the quotes.
        var code = await client.ExecuteAsync(["echo", "a; b"], output, error).WaitAsync(Limit);

        Assert.That(code, Is.EqualTo(0));
        Assert.That(output.ToString(), Is.EqualTo("a; b\n"));
        Assert.That(error.ToString(), Is.Empty);
    }

    [Test]
    public async Task AnEmptyArgumentReachesTheCommand()
    {
        await using var server = new RunningServer();
        await using var client = await server.ConnectAsync();
        using var output = new StringWriter { NewLine = "\n" };
        using var error = new StringWriter { NewLine = "\n" };

        // echo "" : System.CommandLine splits the line into an empty token, which is the value of the argument, without any error.
        var code = await client.ExecuteAsync(["echo", string.Empty], output, error).WaitAsync(Limit);

        Assert.That(code, Is.EqualTo(0));
        Assert.That(error.ToString(), Is.Empty);
        Assert.That(output.ToString(), Is.EqualTo("\n"));
    }

    [Test]
    public async Task ForwardReturnsTheCodeAndTheOutput()
    {
        await using var server = new RunningServer();

        var (code, output, error) = await ForwardAsync(server.PipeName, ["hello"]);

        Assert.That(code, Is.EqualTo(0));
        Assert.That(output, Is.EqualTo("hello\n"));
        Assert.That(error, Is.Empty);
    }

    [Test]
    public async Task ForwardWritesErrorsOnTheErrorOutput()
    {
        await using var server = new RunningServer();

        var (code, output, error) = await ForwardAsync(server.PipeName, ["oops"]);

        Assert.That(code, Is.EqualTo(2));
        Assert.That(error, Is.EqualTo("oops\n"));
        Assert.That(output, Is.Empty);
    }

    [Test]
    public async Task ForwardWithoutServerReturnsNull()
    {
        var pipeName = "cmdstarter-test-" + Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();

        var (code, output, error) = await ForwardAsync(pipeName, ["hello"], timeout: TimeSpan.FromMilliseconds(200));

        stopwatch.Stop();
        Assert.That(code, Is.Null);
        Assert.That(output, Is.Empty);
        Assert.That(error, Is.Empty);
        // Without a server the connection is retried until the timeout: the fallback costs the whole timeout (half of it, to leave the clock some room).
        Assert.That(stopwatch.Elapsed, Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(100)));
    }

    [Test]
    public async Task ForwardWithABusyServerReturnsNullAfterTheTimeout()
    {
        await using var server = new RunningServer();
        await using var first = await server.ConnectAsync();

        var (code, output, error) = await ForwardAsync(server.PipeName, ["hello"], timeout: TimeSpan.FromMilliseconds(500));

        Assert.That(code, Is.Null);
        Assert.That(output, Is.Empty);
        Assert.That(error, Is.Empty);
        Assert.That(await ExecuteHelloAsync(first), Is.EqualTo(0), "the first session still works");
    }

    [Test]
    public async Task ForwardWithoutArgumentsReturnsNullWithoutConnecting()
    {
        await using var server = new RunningServer();

        Assert.That((await ForwardAsync(server.PipeName, [])).Code, Is.Null);

        // The server was not consumed: the next client gets the session.
        await using var client = await server.ConnectAsync();
        Assert.That(await ExecuteHelloAsync(client), Is.EqualTo(0));
    }

    [Test]
    public async Task ForwardOfAnArgumentThatCannotBeWrittenReturnsNull()
    {
        await using var server = new RunningServer();

        Assert.That((await ForwardAsync(server.PipeName, ["echo", "a\"b"])).Code, Is.Null);

        await using var client = await server.ConnectAsync();
        Assert.That(await ExecuteHelloAsync(client), Is.EqualTo(0));
    }

    [Test]
    public void ForwardRefusesNullArguments()
    {
        var pipeName = "cmdstarter-test-" + Guid.NewGuid().ToString("N");
        var console = new CommandConsole(TextReader.Null, new StringWriter(), new StringWriter());

        foreach (var invalid in new[] { string.Empty, "   " })
        {
            Assert.ThrowsAsync<ArgumentException>(async () => await ReplPipeClient.TryForwardAsync(invalid, ["hello"], Limit, console));
        }

        Assert.CatchAsync<ArgumentException>(async () => await ReplPipeClient.TryForwardAsync(null!, ["hello"], Limit, console));
        Assert.ThrowsAsync<ArgumentNullException>(async () => await ReplPipeClient.TryForwardAsync(pipeName, null!, Limit, console));
        Assert.ThrowsAsync<ArgumentNullException>(async () => await ReplPipeClient.TryForwardAsync(pipeName, ["hello"], Limit, null!));

        // A null element is a mistake of the caller, also on the path of the sequence option, which does not join the arguments.
        var element = Assert.ThrowsAsync<ArgumentException>(async () => await ReplPipeClient.TryForwardAsync(pipeName, ["hello", null!], Limit, console));
        Assert.That(element!.Message, Does.Contain("index 1"));
        Assert.ThrowsAsync<ArgumentException>(async () => await ReplPipeClient.TryForwardAsync(pipeName, ["-c", null!], Limit, console, "-c"));
    }

    [Test]
    public async Task ForwardRunsASequence()
    {
        await using var server = new RunningServer();

        // The line is: hello ";" oops. The quoted ';' is an argument and not an operator: the forwarding carries arguments, not a sequence.
        var (code, _, error) = await ForwardAsync(server.PipeName, ["hello", ";", "oops"]);

        // A parse error of System.CommandLine: a code that is not 0 and a message on the error output (the output may hold its help, so it is not checked).
        Assert.That(code, Is.Not.Null.And.Not.EqualTo(0));
        Assert.That(error, Is.Not.Empty);
    }

    [Test]
    public async Task ForwardRunsASequenceThroughTheSequenceOption()
    {
        await using var server = new RunningServer();

        // The line is sent as it is: the session does not need any SequenceOption.
        var (code, output, error) = await ForwardAsync(server.PipeName, ["-c", "hello; oops"], "-c");
        Assert.That(code, Is.EqualTo(2));
        Assert.That(output, Is.EqualTo("hello\n"));
        Assert.That(error, Is.EqualTo("oops\n"));

        // Without the option, the two arguments are written as arguments: -c "hello; oops".
        var without = await ForwardAsync(server.PipeName, ["-c", "hello; oops"]);
        Assert.That(without.Code, Is.Not.Null.And.Not.EqualTo(0));
        Assert.That(without.Error, Is.Not.Empty);

        // A line of REPL may hold double quotes, which no list of arguments can: it does not go through the writing of the arguments.
        var quoted = await ForwardAsync(server.PipeName, ["-c", "echo \"a b\""], "-c");
        Assert.That(quoted.Code, Is.EqualTo(0));
        Assert.That(quoted.Output, Is.EqualTo("a b\n"));
        Assert.That(quoted.Error, Is.Empty);

        // With the option given, other arguments are still written as arguments: a single argument, or two whose first is not the option.
        Assert.That((await ForwardAsync(server.PipeName, ["hello"], "-c")).Output, Is.EqualTo("hello\n"));
        Assert.That((await ForwardAsync(server.PipeName, ["echo", "a b"], "-c")).Output, Is.EqualTo("a b\n"));
    }

    [Test]
    public async Task ForwardOfASequenceLineThatCannotBeSentReturnsNull()
    {
        await using var server = new RunningServer();

        foreach (var line in new[] { "hello\nhello", "hello\rhello", "   " })
        {
            Assert.That((await ForwardAsync(server.PipeName, ["-c", line], "-c")).Code, Is.Null, $"line '{line.Replace("\n", "\\n", StringComparison.Ordinal).Replace("\r", "\\r", StringComparison.Ordinal)}'");
        }

        // Nothing was consumed: the next client gets the session.
        await using var client = await server.ConnectAsync();
        Assert.That(await ExecuteHelloAsync(client), Is.EqualTo(0));
    }

    [Test]
    public async Task ForwardOfAnExitWordEndsTheSession()
    {
        await using var server = new RunningServer();

        var exit = await ForwardAsync(server.PipeName, ["exit"]);
        Assert.That(exit.Code, Is.EqualTo(0));
        Assert.That(exit.Output, Is.Empty);
        Assert.That(exit.Error, Is.Empty);

        // The session ended, so the server accepts the next client.
        var hello = await ForwardAsync(server.PipeName, ["hello"]);
        Assert.That(hello.Code, Is.EqualTo(0));
        Assert.That(hello.Output, Is.EqualTo("hello\n"));
    }

    [Test]
    public async Task ForwardWithoutBuiltInWordsRunsTheArgumentsAsStartDoes()
    {
        await using var server = new RunningServer(CreateSessionWithoutBuiltInWords);

        foreach (var word in new[] { "exit", "help" })
        {
            var forwarded = await ForwardAsync(server.PipeName, [word]);

            using var localOutput = new StringWriter { NewLine = "\n" };
            using var localError = new StringWriter { NewLine = "\n" };
            var local = await CreateSessionWithoutBuiltInWords().Start([word], new CommandConsole(TextReader.Null, localOutput, localError)).WaitAsync(Limit);

            Assert.That(forwarded.Code, Is.Not.Null.And.Not.EqualTo(0), word);
            Assert.That(forwarded.Code, Is.EqualTo(local), word);
        }

        Assert.That((await ForwardAsync(server.PipeName, ["hello"])).Code, Is.EqualTo(0));
    }

    private static ReplStarter CreateSessionWithoutBuiltInWords()
    {
        var session = CreateSession();
        session.ExitCommands = [];
        session.HelpCommands = [];
        return session;
    }

    private static async Task<int> ExecuteHelloAsync(ReplPipeClient client)
    {
        using var output = new StringWriter { NewLine = "\n" };
        using var error = new StringWriter { NewLine = "\n" };
        return await client.ExecuteAsync("hello", output, error).WaitAsync(Limit);
    }

    private static async Task<(int? Code, string Output, string Error)> ForwardAsync(string pipeName, IReadOnlyList<string> arguments, string? sequenceOption = null, TimeSpan? timeout = null)
    {
        using var output = new StringWriter { NewLine = "\n" };
        using var error = new StringWriter { NewLine = "\n" };
        var console = new CommandConsole(TextReader.Null, output, error);
        var code = await ReplPipeClient.TryForwardAsync(pipeName, arguments, timeout ?? Limit, console, sequenceOption).WaitAsync(Limit);
        return (code, output.ToString(), error.ToString());
    }
}
