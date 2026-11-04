using System.CommandLine;
using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Tests.Commands.Sequences;

namespace CyberInternauts.CmdStarter.Tests;

/// <summary>Reaching System.CommandLine from the Starter: the root command, commands added by hand, the invocation configuration.</summary>
[Category("* All *")]
[Category("SystemCommandLine")]
public class SystemCommandLineAccess
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [SetUp]
    public void Setup() => SequenceLog.Entries.Clear();

    [Test]
    public async Task ManualCommandRunsUnderRootCommand()
    {
        var starter = CreateStarter();
        starter.InstantiateCommands();
        var manual = new Command("manual");
        manual.SetAction(_ => 42);
        starter.RootCommand.Subcommands.Add(manual);

        Assert.That(await starter.Start("manual"), Is.EqualTo(42));
        Assert.That(await starter.Start("ok"), Is.EqualTo(0));
        Assert.That(SequenceLog.Entries, Is.EqualTo(new[] { "ok" }));
    }

    [Test]
    public async Task ChangingAFilterReplacesRootCommandAndDiscardsManualCommand()
    {
        var starter = CreateStarter();
        starter.InstantiateCommands();
        var root = starter.RootCommand;
        var manual = new Command("manual");
        manual.SetAction(_ => 42);
        root.Subcommands.Add(manual);
        Assert.That(await starter.Start("manual"), Is.EqualTo(42));

        starter.Namespaces = ImmutableList.Create(typeof(Ok).Namespace!);

        Assert.That(starter.RootCommand, Is.Not.SameAs(root));
        Assert.That(await starter.Start("manual"), Is.Not.EqualTo(0).And.Not.EqualTo(42));
        Assert.That(await starter.Start("ok"), Is.EqualTo(0));
    }

    [Test]
    public async Task InvocationConfigurationIsNewForEachExecution()
    {
        var starter = new ConfigurationStarter { Namespaces = ImmutableList.Create(typeof(Ok).Namespace!) };
        using var firstOutput = new StringWriter();
        using var secondOutput = new StringWriter();
        var first = new CommandConsole(new StringReader(string.Empty), firstOutput, new StringWriter());
        var second = new CommandConsole(new StringReader(string.Empty), secondOutput, new StringWriter());

        Assert.That(await starter.Start("emit", first), Is.EqualTo(0));
        Assert.That(await starter.Start("emit", second), Is.EqualTo(0));

        Assert.That(starter.Created, Has.Count.EqualTo(2));
        Assert.That(starter.Created[0], Is.Not.SameAs(starter.Created[1]));
        Assert.That(firstOutput.ToString().ReplaceLineEndings("\n"), Is.EqualTo("b\na\nc\n"));
        Assert.That(secondOutput.ToString().ReplaceLineEndings("\n"), Is.EqualTo("b\na\nc\n"));
    }

    private static Starter CreateStarter() => new()
    {
        Namespaces = ImmutableList.Create(typeof(Ok).Namespace!)
    };

    // A subclass that customizes System.CommandLine's invocation: it keeps what it creates, one new configuration per call.
    private sealed class ConfigurationStarter : Starter
    {
        internal List<InvocationConfiguration> Created { get; } = new();

        protected override InvocationConfiguration? CreateInvocationConfiguration()
        {
            var configuration = new InvocationConfiguration();
            Created.Add(configuration);
            return configuration;
        }
    }
}
