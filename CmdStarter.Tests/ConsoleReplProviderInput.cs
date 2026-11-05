using CyberInternauts.CmdStarter.Repl;

namespace CyberInternauts.CmdStarter.Tests;

/// <summary>The reading of <see cref="ConsoleReplProvider"/> that goes through the standard streams as they are.</summary>
[Category("* All *")]
[Category("REPL")]
public class ConsoleReplProviderInput
{
    [Test]
    public void HistorySizeMustBeAtLeastOne()
    {
        foreach (var invalid in new[] { 0, -5 })
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _ = new ConsoleReplProvider { HistorySize = invalid });
            Assert.That(exception!.ParamName, Is.EqualTo("value"));
        }

        Assert.That(new ConsoleReplProvider().HistorySize, Is.EqualTo(100));
        Assert.That(new ConsoleReplProvider { HistorySize = 7 }.HistorySize, Is.EqualTo(7));
    }

    [Test]
    public async Task WithoutLineEditingThePromptAndTheLineGoThroughTheStandardStreams()
    {
        var originalIn = Console.In;
        var originalOut = Console.Out;
        using var input = new StringReader("first\nsecond\n");
        using var output = new StringWriter();
        try
        {
            Console.SetIn(input);
            Console.SetOut(output);
            var provider = new ConsoleReplProvider { LineEditing = false, Prompt = "? " };

            var first = await provider.GetInputAsync(CancellationToken.None);
            var second = await provider.GetInputAsync(CancellationToken.None);
            var end = await provider.GetInputAsync(CancellationToken.None);

            Assert.That(first, Is.EqualTo("first"));
            Assert.That(second, Is.EqualTo("second"));
            Assert.That(end, Is.Null);
            Assert.That(output.ToString(), Is.EqualTo("? ? ? "));
        }
        finally
        {
            Console.SetIn(originalIn);
            Console.SetOut(originalOut);
        }
    }
}
