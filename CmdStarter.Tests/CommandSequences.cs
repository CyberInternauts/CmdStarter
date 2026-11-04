using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Exceptions;
using CyberInternauts.CmdStarter.Repl;
using CyberInternauts.CmdStarter.Sequences;
using CyberInternauts.CmdStarter.Tests.Commands.Repl;
using CyberInternauts.CmdStarter.Tests.Commands.Sequences;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("Sequences")]
public class CommandSequences
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [SetUp]
    public void Setup() => SequenceLog.Entries.Clear();

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
    public void SingleAmpersandIsText()
    {
        var steps = Parse("a & b");

        Assert.That(steps.Select(step => step.CommandLine), Is.EqualTo(new[] { "a & b" }));
        Assert.That(steps.Select(step => step.Operator), Is.EqualTo(new[] { SequenceOperator.None }));
    }

    [Test]
    public void SplitsPipelines()
    {
        var steps = Parse("a | b \"c|d\" || e");

        Assert.That(steps, Has.Count.EqualTo(2));
        Assert.That(steps[0].CommandLine, Is.EqualTo("a | b \"c|d\""));
        Assert.That(steps[0].Operator, Is.EqualTo(SequenceOperator.None));
        Assert.That(steps[0].Pipeline, Is.EqualTo(new[] { "a", "b \"c|d\"" }));
        Assert.That(steps[1].Operator, Is.EqualTo(SequenceOperator.OnFailure));
        Assert.That(steps[1].Pipeline, Is.EqualTo(new[] { "e" }));
    }

    [Test]
    public void InvalidPipelinesAreRejected()
    {
        var invalidLines = new[] { "| a", "a |", "a | | b", "a || | b", "a | && b" };

        Assert.Multiple(() =>
        {
            foreach (var line in invalidLines)
            {
                Assert.That(CommandSequence.TryParse(line, out var sequence, out var error), Is.False, line);
                Assert.That(sequence, Is.Null, line);
                Assert.That(error, Does.Contain("'|'"), line);
            }
        });
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

    [Test]
    public async Task StartSequenceRunsStepsAndReturnsLastCode()
    {
        var starter = CreateStarter();

        Assert.That(await starter.StartSequence("ok; fail"), Is.EqualTo(3));
        Assert.That(SequenceLog.Entries, Is.EqualTo(new[] { "ok", "fail" }));
    }

    [Test]
    public async Task AndSkipsAfterFailure()
    {
        var starter = CreateStarter();

        Assert.That(await starter.StartSequence("fail && ok"), Is.EqualTo(3));
        Assert.That(SequenceLog.Entries, Is.EqualTo(new[] { "fail" }));

        SequenceLog.Entries.Clear();
        Assert.That(await starter.StartSequence("ok && fail && other"), Is.EqualTo(3));
        Assert.That(SequenceLog.Entries, Is.EqualTo(new[] { "ok", "fail" }));
    }

    [Test]
    public async Task OrRunsAfterFailureOnly()
    {
        var starter = CreateStarter();

        Assert.That(await starter.StartSequence("fail || ok"), Is.EqualTo(0));
        Assert.That(SequenceLog.Entries, Is.EqualTo(new[] { "fail", "ok" }));

        SequenceLog.Entries.Clear();
        Assert.That(await starter.StartSequence("ok || fail"), Is.EqualTo(0));
        Assert.That(SequenceLog.Entries, Is.EqualTo(new[] { "ok" }));
    }

    [Test]
    public async Task InvalidSequenceReturnsOneAndWritesError()
    {
        var starter = CreateStarter();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var console = new CommandConsole(new StringReader(string.Empty), output, error);

        Assert.That(await starter.StartSequence("ok &&", console), Is.EqualTo(1));
        Assert.That(SequenceLog.Entries, Is.Empty);
        Assert.That(error.ToString(), Is.Not.Empty);
    }

    [Test]
    public async Task UnclosedQuoteWithoutOperatorRunsAsBefore()
    {
        var starter = CreateStarter();
        using var startError = new StringWriter();

        // What a plain Start does with an unclosed quote: System.CommandLine cannot read a command token from it, so nothing runs and the code is not 0.
        var startCode = await starter.Start("\"ok", ConsoleWith(startError));
        Assert.That(startCode, Is.Not.EqualTo(0));
        Assert.That(SequenceLog.Entries, Is.Empty);

        // StartSequence gives the same code, and reports the System.CommandLine error, not an invalid sequence.
        using var sequenceError = new StringWriter();
        Assert.That(await starter.StartSequence("\"ok", ConsoleWith(sequenceError)), Is.EqualTo(startCode));
        Assert.That(sequenceError.ToString(), Is.EqualTo(startError.ToString()));
        Assert.That(sequenceError.ToString(), Does.Not.Contain("Unclosed double quote"));

        // The REPL loop hands the line to Start: the event is raised once, with the same code.
        using var replError = new StringWriter();
        var codes = new List<int>();
        var repl = CreateRepl("\"ok");
        repl.DefaultConsole = ConsoleWith(replError);
        repl.OnCommandExecuted += (_, eventArgs) => codes.Add(eventArgs.ReturnCode);
        await repl.Launch();
        Assert.That(codes, Is.EqualTo(new[] { startCode }));
        Assert.That(replError.ToString(), Does.Not.Contain("Unclosed double quote"));
    }

    private static CommandConsole ConsoleWith(StringWriter error) => new(new StringReader(string.Empty), new StringWriter(), error);

    [Test]
    public async Task SequenceOptionRunsSequence()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var console = new CommandConsole(new StringReader(string.Empty), output, error);
        var starter = CreateStarter();
        starter.SequenceOption = "-c";

        Assert.That(await starter.Start(new[] { "-c", "ok; other" }), Is.EqualTo(0));
        Assert.That(SequenceLog.Entries, Is.EqualTo(new[] { "ok", "other" }));

        // Without the option, "-c" is an unknown option, as before.
        SequenceLog.Entries.Clear();
        var plainStarter = CreateStarter();
        Assert.That(await plainStarter.Start(new[] { "-c", "ok" }, console), Is.Not.EqualTo(0));
        Assert.That(SequenceLog.Entries, Is.Empty);
    }

    [Test]
    public void SequenceOptionConflictThrows()
    {
        var starter = CreateStarter();
        starter.SequenceOption = "ok";

        var exception = Assert.Throws<InvalidAttributeException>(() => starter.InstantiateCommands());
        Assert.That(exception!.Message, Does.Contain("ok"));
    }

    [Test]
    public async Task ReplRunsSequences()
    {
        var executed = 0;
        var repl = CreateRepl("ok; other", "fail || ok");
        repl.OnCommandExecuted += (_, _) => executed++;

        await repl.Launch();

        Assert.That(SequenceLog.Entries, Is.EqualTo(new[] { "ok", "other", "fail", "ok" }));
        Assert.That(executed, Is.EqualTo(4));
    }

    [Test]
    public async Task ExitInsideSequenceEndsLoop()
    {
        var repl = CreateRepl("ok; exit; other", "ok");

        await repl.Launch();

        Assert.That(SequenceLog.Entries, Is.EqualTo(new[] { "ok" }));
    }

    [Test]
    public void CompletionUsesCurrentStep()
    {
        var repl = CreateRepl();

        var completions = repl.GetCompletions("ok; ot", 6);
        Assert.That(completions, Does.Contain("other"));
        Assert.That(completions, Does.Not.Contain("ok"));

        Assert.That(repl.GetCompletions("ok; ", 4), Does.Contain("exit"));
    }

    [Test]
    public void CompletionAfterTrailingOperator()
    {
        Assert.That(CreateRepl().GetCompletions("ok && ", 6), Does.Contain("other"));
    }

    [Test]
    public void CompletionInsideOperatorUsesWholeLine()
    {
        Assert.That(CreateRepl().GetCompletions("ok &&", 4), Does.Not.Contain("other"));
    }

    [Test]
    public void CompletionAfterHelpWordInStepListsCommands()
    {
        var completions = CreateRepl().GetCompletions("ok; help ", 9);

        Assert.That(completions, Does.Contain("other"));
        Assert.That(completions, Does.Not.Contain("exit"));
    }

    private static Starter CreateStarter() => new()
    {
        Namespaces = ImmutableList.Create(typeof(Ok).Namespace!)
    };

    private static ReplStarter CreateRepl(params string?[] lines) => new(new ScriptedInputProvider(lines))
    {
        Namespaces = ImmutableList.Create(typeof(Ok).Namespace!)
    };

    private static IReadOnlyList<CommandSequenceStep> Parse(string line)
    {
        Assert.That(CommandSequence.TryParse(line, out var sequence, out var error), Is.True, error);
        Assert.That(error, Is.Null);
        return sequence!.Steps;
    }
}
