using CyberInternauts.CmdStarter.Sequences;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("Sequences")]
public class CommandSequences
{
    [Test]
    public void SplitsOnSemicolonsOutsideQuotes()
    {
        var steps = Parse("a; \"b;c\" d ; e");

        Assert.That(steps.Select(step => step.CommandLine), Is.EqualTo(new[] { "a", "\"b;c\" d", "e" }));
        Assert.That(steps.Select(step => step.Operator), Is.EqualTo(new[] { SequenceOperator.None, SequenceOperator.Always, SequenceOperator.Always }));
    }

    [Test]
    public void ReadsAndOrOperators()
    {
        var steps = Parse("a && b || c");

        Assert.That(steps.Select(step => step.CommandLine), Is.EqualTo(new[] { "a", "b", "c" }));
        Assert.That(steps.Select(step => step.Operator), Is.EqualTo(new[] { SequenceOperator.None, SequenceOperator.OnSuccess, SequenceOperator.OnFailure }));
    }

    [Test]
    public void SinglePipeAndAmpersandAreText()
    {
        var steps = Parse("a | b & c");

        Assert.That(steps.Select(step => step.CommandLine), Is.EqualTo(new[] { "a | b & c" }));
        Assert.That(steps.Select(step => step.Operator), Is.EqualTo(new[] { SequenceOperator.None }));
    }

    [Test]
    public void EmptyStepsAfterSemicolonAreSkipped()
    {
        var steps = Parse("a;;b;");

        Assert.That(steps.Select(step => step.CommandLine), Is.EqualTo(new[] { "a", "b" }));
        Assert.That(steps.Select(step => step.Operator), Is.EqualTo(new[] { SequenceOperator.None, SequenceOperator.Always }));
    }

    [Test]
    public void InvalidSequencesAreRejected()
    {
        var invalidLines = new (string Line, string ExpectedInMessage)[]
        {
            ("&& a", "&&"),
            ("|| a", "||"),
            ("a &&", "&&"),
            ("a || || b", "||"),
            ("a && ; b", "&&"),
            ("a ; && b", "&&"),
            ("a \"b", "quote")
        };

        Assert.Multiple(() =>
        {
            foreach (var (line, expectedInMessage) in invalidLines)
            {
                Assert.That(CommandSequence.TryParse(line, out var sequence, out var error), Is.False, line);
                Assert.That(sequence, Is.Null, line);
                Assert.That(error, Does.Contain(expectedInMessage), line);
            }

            // A blank line is valid and has no step; it is not an error.
            foreach (var blankLine in new[] { string.Empty, "   ", " ; ; " })
            {
                Assert.That(CommandSequence.TryParse(blankLine, out var sequence, out var error), Is.True, blankLine);
                Assert.That(error, Is.Null, blankLine);
                Assert.That(sequence!.Steps, Is.Empty, blankLine);
            }
        });
    }

    private static IReadOnlyList<CommandSequenceStep> Parse(string line)
    {
        Assert.That(CommandSequence.TryParse(line, out var sequence, out var error), Is.True, error);
        Assert.That(error, Is.Null);
        return sequence!.Steps;
    }
}
