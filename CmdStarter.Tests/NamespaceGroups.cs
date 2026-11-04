using CyberInternauts.CmdStarter.Tests.Commands.NamespaceGroups;
using CyberInternauts.CmdStarter.Loader;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("NamespaceGroups")]
public class NamespaceGroups
{
    private const string NamespaceName = "CyberInternauts.CmdStarter.Tests.Commands.NamespaceGroups";

    [OneTimeSetUp]
    public void GlobalSetup()
    {
        TestsCommon.GlobalSetup();
    }

    [Test]
    public void DisabledByDefaultKeepsConflict()
    {
        var starter = CreateStarter();

        Assert.Throws<InvalidAttributeException>(() => starter.InstantiateCommands());
    }

    [Test]
    public async Task NamespaceSegmentsBecomeGroups()
    {
        var starter = CreateStarter();
        starter.GroupsByNamespace = true;

        Assert.That(await starter.Start("users list"), Is.EqualTo(43));
        Assert.That(await starter.Start("groups list"), Is.EqualTo(44));
        Assert.That(await starter.Start("admin security reset"), Is.EqualTo(45));
        Assert.That(await starter.Start("status"), Is.EqualTo(41));
        Assert.That(await starter.Start("about"), Is.EqualTo(42));
    }

    [Test]
    public async Task GroupReusesCommandOfSameName()
    {
        var starter = CreateStarter();
        starter.GroupsByNamespace = true;

        Assert.That(await starter.Start("tools cleanup"), Is.EqualTo(46));
        Assert.That(starter.RootCommand.Subcommands.Count(command => string.Equals(command.Name, "tools", StringComparison.Ordinal)), Is.EqualTo(1));
    }

    [Test]
    public async Task ParentAttributeIsNotGrouped()
    {
        var starter = CreateStarter();
        starter.GroupsByNamespace = true;

        Assert.That(await starter.Start("host attached"), Is.EqualTo(47));
        Assert.That(starter.RootCommand.Subcommands.Any(command => string.Equals(command.Name, "deep", StringComparison.Ordinal)), Is.False);
    }

    [Test]
    public async Task MethodCommandsJoinNamespaceGroups()
    {
        var starter = CreateStarter();
        starter.GroupsByNamespace = true;

        Assert.That(await starter.Start("users export"), Is.EqualTo(48));
    }

    [Test]
    public async Task NamespaceParentIsNotGroupedByItsOwnSegment()
    {
        var starter = CreateStarter();
        starter.GroupsByNamespace = true;

        Assert.That(await starter.Start("remote prune"), Is.EqualTo(49));
        var remote = starter.RootCommand.Subcommands.Single(command => string.Equals(command.Name, "remote", StringComparison.Ordinal));
        Assert.That(remote.Subcommands.Any(command => string.Equals(command.Name, "remote", StringComparison.Ordinal)), Is.False);
    }

    [Test]
    public void OnlyAttributesModeIgnoresOption()
    {
        var starter = CreateStarter();
        starter.ClassesBuildingMode = ClassesBuildingMode.OnlyAttributes;
        starter.GroupsByNamespace = true;

        Assert.Throws<InvalidAttributeException>(() => starter.InstantiateCommands());
    }

    [Test]
    public void DefinedTreeIgnoresOption()
    {
        var starter = CreateStarter();
        starter.GroupsByNamespace = true;
        starter.UseTree(tree => tree.Add<Status>().Add<About>());

        starter.InstantiateCommands();

        Assert.That(starter.RootCommand.Subcommands.Select(command => command.Name).ToHashSet(StringComparer.Ordinal),
            Is.EquivalentTo(new[] { "status", "about", "users" }));
    }

    [Test]
    public async Task ChangingOptionRebuildsTree()
    {
        var starter = CreateStarter();

        Assert.Throws<InvalidAttributeException>(() => starter.InstantiateCommands());
        starter.GroupsByNamespace = true;

        Assert.That(await starter.Start("users list"), Is.EqualTo(43));
    }

    private static Starter CreateStarter() => new(new[] { NamespaceName });
}
