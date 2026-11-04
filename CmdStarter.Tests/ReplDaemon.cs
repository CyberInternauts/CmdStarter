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
}
