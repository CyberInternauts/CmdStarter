using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Tests.Commands.NamespaceGroups;
using CyberInternauts.CmdStarter.Tests.Commands.Sequences;

namespace CyberInternauts.CmdStarter.Tests;

/// <summary>
/// Starter.CommandsTypes can be set by the application: the types are checked, and the list stands for the discovery of the class commands
/// when the discovery has not run yet.
/// </summary>
[Category("* All *")]
[Category("CmdStarter")]
[Category("CommandsTypes")]
public class CommandsTypesSetter
{
    private static readonly string SequencesNamespace = typeof(Ok).Namespace!;

    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [Test]
    public void TypeWithoutNamespaceIsRejectedWithItsName()
    {
        var starter = CreateDiscoveredStarter();
        var before = starter.CommandsTypes;

        var exception = Assert.Throws<ArgumentException>(() => starter.CommandsTypes = ImmutableList.Create(typeof(Ok), typeof(GlobalNamespaceCommand)));

        Assert.That(exception!.Message, Does.Contain(nameof(GlobalNamespaceCommand)).And.Contain("no namespace"));
        Assert.That(exception.ParamName, Is.EqualTo("value"), "the exception names the parameter of the setter");
        Assert.That(starter.CommandsTypes, Is.EqualTo(before), "the list is unchanged when the setter throws");
    }

    [Test]
    public void TypeListedTwiceIsRejectedWithItsName()
    {
        var starter = CreateDiscoveredStarter();
        var before = starter.CommandsTypes;

        var exception = Assert.Throws<ArgumentException>(() => starter.CommandsTypes = ImmutableList.Create(typeof(Ok), typeof(Other), typeof(Ok)));

        Assert.That(exception!.Message, Does.Contain(typeof(Ok).FullName!).And.Contain("listed twice"));
        Assert.That(starter.CommandsTypes, Is.EqualTo(before));
    }

    [Test]
    public void TypesThatAreNotCommandClassesAreRejectedWithTheirName()
    {
        var starter = CreateDiscoveredStarter();
        var before = starter.CommandsTypes;

        Assert.Multiple(() =>
        {
            foreach (var invalid in new[] { typeof(string), typeof(AbstractCommand), typeof(IDisposable) })
            {
                var exception = Assert.Throws<ArgumentException>(() => starter.CommandsTypes = ImmutableList.Create(typeof(Ok), invalid), invalid.Name);
                Assert.That(exception!.Message, Does.Contain(invalid.FullName!), invalid.Name);
                Assert.That(starter.CommandsTypes, Is.EqualTo(before), invalid.Name);
            }
        });
    }

    [Test]
    public void NullElementIsRejected()
    {
        var starter = CreateDiscoveredStarter();

        var exception = Assert.Throws<ArgumentException>(() => starter.CommandsTypes = ImmutableList.Create(typeof(Ok), null!));

        Assert.That(exception!.Message, Does.Contain("null"));
    }

    [Test]
    public void ListSetBeforeTheDiscoveryIsTheWholeDiscovery()
    {
        // A discovery would find the default commands of the tests, and method commands too.
        var starter = TestsCommon.CreateCmdStarter();

        starter.CommandsTypes = ImmutableList.Create(typeof(Ok), typeof(Other));
        starter.InstantiateCommands();

        Assert.That(starter.RootCommand.Subcommands.Select(command => command.Name), Is.EqualTo(new[] { "ok", "other" }));
        Assert.That(starter.CommandsTypes, Is.EqualTo(new[] { typeof(Ok), typeof(Other) }));
        Assert.That(starter.MethodCommandsTypes, Is.Empty);
    }

    [Test]
    public async Task ListSetBeforeTheDiscoveryRunsItsCommands()
    {
        var starter = TestsCommon.CreateCmdStarter();
        starter.CommandsTypes = ImmutableList.Create(typeof(Fail), typeof(Other));
        SequenceLog.Entries.Clear();

        Assert.That(await starter.Start("fail"), Is.EqualTo(3));
        Assert.That(SequenceLog.Entries, Is.EqualTo(new[] { "fail" }));
    }

    [Test]
    public void FiltersSetAfterTheListResumeTheDiscovery()
    {
        var starter = TestsCommon.CreateCmdStarter();
        starter.CommandsTypes = [typeof(Ok)];

        starter.Namespaces = [SequencesNamespace];
        starter.InstantiateCommands();

        Assert.That(starter.RootCommand.Subcommands.Select(command => command.Name), Does.Contain("other").And.Contain("fail"));
    }

    [Test]
    public void EmptyListGoesBackToTheDiscovery()
    {
        var starter = new Starter { Namespaces = [SequencesNamespace] };
        starter.CommandsTypes = [typeof(Ok)];

        starter.CommandsTypes = [];
        starter.InstantiateCommands();

        Assert.That(starter.RootCommand.Subcommands.Select(command => command.Name), Does.Contain("other").And.Contain("fail"));
    }

    [Test]
    public async Task ListAdjustedAfterTheDiscoveryKeepsTheMethodCommands()
    {
        var starter = new Starter
        {
            Namespaces = [typeof(Status).Namespace!],
            Classes = ImmutableList.Create(typeof(Status).FullName!, typeof(About).FullName!, typeof(Commands.NamespaceGroups.Methods.NamespaceGroupMethods).FullName!)
        };
        starter.FindCommandsTypes();
        Assert.That(starter.MethodCommandsTypes, Is.Not.Empty);

        starter.CommandsTypes = [typeof(Status)];

        Assert.That(await starter.Start("users export"), Is.EqualTo(48));
        Assert.That(starter.RootCommand.Subcommands.Select(command => command.Name), Is.EquivalentTo(new[] { "status", "users" }));
    }

    private static Starter CreateDiscoveredStarter()
    {
        var starter = new Starter { Namespaces = [SequencesNamespace] };
        starter.FindCommandsTypes();
        return starter;
    }

    private abstract class AbstractCommand : StarterCommand
    {
    }
}
