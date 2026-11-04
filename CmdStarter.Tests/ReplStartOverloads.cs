using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Repl;
using CyberInternauts.CmdStarter.Tests.Commands.Repl;
using CyberInternauts.CmdStarter.Tests.Commands.Sequences;

namespace CyberInternauts.CmdStarter.Tests;

/// <summary>
/// The Start and StartSequence overloads of ReplStarter raise OnCommandExecuted for every command they run, with or without a console;
/// the constructors with namespaces filter the commands and share the completions; the REPL line reports its errors on the right stream.
/// </summary>
[Category("* All *")]
[Category("REPL")]
public class ReplStartOverloads
{
    private static readonly string SequencesNamespace = typeof(Ok).Namespace!;

    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [SetUp]
    public void Setup() => SequenceLog.Entries.Clear();

    [Test]
    public void ConstructorWithNamespaceArrayFiltersCommandsAndSharesCompletions()
    {
        var provider = new ConsoleReplProvider();

        var repl = new ReplStarter(provider, new[] { SequencesNamespace });
        repl.InstantiateCommands();

        Assert.That(repl.Namespaces, Is.EqualTo(new[] { SequencesNamespace }));
        Assert.That(repl.FindCommand<Ok>(), Is.Not.Null);
        Assert.That(provider.CompletionSource!("ot", 2), Does.Contain("other"));
    }

    [Test]
    public void ConstructorWithNamespaceListFiltersCommandsAndSharesCompletions()
    {
        var provider = new ConsoleReplProvider();

        var repl = new ReplStarter(provider, new List<string> { SequencesNamespace });
        repl.InstantiateCommands();

        Assert.That(repl.Namespaces, Is.EqualTo(new[] { SequencesNamespace }));
        Assert.That(repl.FindCommand<Ok>(), Is.Not.Null);
        Assert.That(provider.CompletionSource!("ot", 2), Does.Contain("other"));
    }

    [Test]
    public async Task StartWithoutTokenRaisesTheEventForArraysAndStrings()
    {
        var codes = new List<int>();
        var repl = CreateRepl();
        repl.OnCommandExecuted += (_, eventArgs) => codes.Add(eventArgs.ReturnCode);

        Assert.That(await repl.Start(new[] { "fail" }), Is.EqualTo(3));
        Assert.That(await repl.Start("ok"), Is.EqualTo(0));

        Assert.That(codes, Is.EqualTo(new[] { 3, 0 }));
        Assert.That(SequenceLog.Entries, Is.EqualTo(new[] { "fail", "ok" }));
    }

    [Test]
    public async Task StartWithConsoleRaisesTheEventForArraysAndStrings()
    {
        var codes = new List<int>();
        var repl = CreateRepl();
        repl.OnCommandExecuted += (_, eventArgs) => codes.Add(eventArgs.ReturnCode);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var console = ConsoleWith(output, error);

        Assert.That(await repl.Start(new[] { "fail" }, console), Is.EqualTo(3));
        Assert.That(await repl.Start("ok", console), Is.EqualTo(0));

        Assert.That(codes, Is.EqualTo(new[] { 3, 0 }));
    }

    [Test]
    public async Task SequenceOptionRaisesTheEventForEveryStep()
    {
        var codes = new List<int>();
        var repl = CreateRepl();
        repl.SequenceOption = "-c";
        repl.OnCommandExecuted += (_, eventArgs) => codes.Add(eventArgs.ReturnCode);

        Assert.That(await repl.Start(new[] { "-c", "fail; ok" }), Is.EqualTo(0));

        Assert.That(SequenceLog.Entries, Is.EqualTo(new[] { "fail", "ok" }));
        Assert.That(codes, Is.EqualTo(new[] { 3, 0 }));
    }

    [Test]
    public async Task SequenceOptionWithConsoleRaisesTheEventForEveryCommandOfThePipeline()
    {
        var codes = new List<int>();
        var repl = CreateRepl();
        repl.SequenceOption = "-c";
        repl.OnCommandExecuted += (_, eventArgs) => codes.Add(eventArgs.ReturnCode);
        using var output = new StringWriter();
        using var error = new StringWriter();

        Assert.That(await repl.Start(new[] { "-c", "emit | sort" }, ConsoleWith(output, error)), Is.EqualTo(0));

        Assert.That(output.ToString().ReplaceLineEndings("\n"), Is.EqualTo("a\nb\nc\n"));
        Assert.That(codes, Is.EqualTo(new[] { 0, 0 }));
    }

    [Test]
    public async Task StartSequenceRaisesTheEventForEveryCommand()
    {
        var codes = new List<int>();
        var repl = CreateRepl();
        using var output = new StringWriter();
        using var error = new StringWriter();
        repl.DefaultConsole = ConsoleWith(output, error);
        repl.OnCommandExecuted += (_, eventArgs) => codes.Add(eventArgs.ReturnCode);

        // Single commands run as plain Starts, the commands of a pipeline run with the console of the pipeline.
        Assert.That(await repl.StartSequence("fail; ok"), Is.EqualTo(0));
        Assert.That(await repl.StartSequence("emit | sort"), Is.EqualTo(0));

        Assert.That(codes, Is.EqualTo(new[] { 3, 0, 0, 0 }));
        Assert.That(output.ToString().ReplaceLineEndings("\n"), Is.EqualTo("a\nb\nc\n"));
    }

    [Test]
    public async Task StartSequenceWithConsoleRaisesTheEventForEveryCommand()
    {
        var codes = new List<int>();
        var repl = CreateRepl();
        repl.OnCommandExecuted += (_, eventArgs) => codes.Add(eventArgs.ReturnCode);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var console = ConsoleWith(output, error);

        Assert.That(await repl.StartSequence("fail; ok", console), Is.EqualTo(0));
        Assert.That(await repl.StartSequence("emit | sort", console), Is.EqualTo(0));

        Assert.That(codes, Is.EqualTo(new[] { 3, 0, 0, 0 }));
        Assert.That(output.ToString().ReplaceLineEndings("\n"), Is.EqualTo("a\nb\nc\n"));
    }

    [Test]
    public async Task InvalidSequenceLineWritesToTheDefaultConsoleError()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var repl = CreateRepl("ok &&");
        repl.DefaultConsole = ConsoleWith(output, error);

        await repl.Launch();

        Assert.That(error.ToString(), Does.Contain("&&"));
        Assert.That(SequenceLog.Entries, Is.Empty);
    }

    [Test]
    public async Task InvalidSequenceLineWritesToTheProcessErrorWithoutDefaultConsole()
    {
        var previousError = Console.Error;
        using var error = new StringWriter();
        var repl = CreateRepl("ok &&");
        try
        {
            Console.SetError(error);

            await repl.Launch();
        }
        finally
        {
            Console.SetError(previousError);
        }

        Assert.That(error.ToString(), Does.Contain("&&"));
        Assert.That(SequenceLog.Entries, Is.Empty);
    }

    [Test]
    public async Task PipedExitWordWritesToTheProcessErrorWithoutDefaultConsole()
    {
        var previousError = Console.Error;
        using var error = new StringWriter();
        var repl = CreateRepl("ok | exit", "other");
        try
        {
            Console.SetError(error);

            await repl.Launch();
        }
        finally
        {
            Console.SetError(previousError);
        }

        Assert.That(error.ToString(), Does.Contain("cannot be piped"));
        Assert.That(SequenceLog.Entries, Is.EqualTo(new[] { "other" }));
    }

    [Test]
    public async Task LineOfOnlySeparatorsRunsLikeAPlainStart()
    {
        using var output = new StringWriter();
        using var plainError = new StringWriter();
        var plainCode = await new Starter { Namespaces = ImmutableList.Create(SequencesNamespace) }.Start("; ;", ConsoleWith(output, plainError));
        var codes = new List<int>();
        using var replError = new StringWriter();
        var repl = CreateRepl(" ; ; ");
        repl.DefaultConsole = ConsoleWith(output, replError);
        repl.OnCommandExecuted += (_, eventArgs) => codes.Add(eventArgs.ReturnCode);

        await repl.Launch();

        Assert.That(codes, Is.EqualTo(new[] { plainCode }));
        Assert.That(replError.ToString(), Is.EqualTo(plainError.ToString()));
        Assert.That(SequenceLog.Entries, Is.Empty);
    }

    [Test]
    public void CompletionInAStepBeforeAnOperatorUsesThatStep()
    {
        Assert.That(CreateRepl().GetCompletions("ot; fail", 2), Does.Contain("other"));
    }

    private static CommandConsole ConsoleWith(StringWriter output, StringWriter error) => new(new StringReader(string.Empty), output, error);

    private static ReplStarter CreateRepl(params string?[] lines) => new(new ScriptedInputProvider(lines))
    {
        Namespaces = ImmutableList.Create(SequencesNamespace)
    };
}
