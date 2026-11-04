using CyberInternauts.CmdStarter.Tests.Commands.MethodGroups;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("Methods")]
public sealed class MethodGroups
{
    [Test]
    public async Task NestsGroupUnderExplicitGroup()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start("admin users create"), Is.EqualTo(11));
        Assert.That(await starter.Start("admin status"), Is.EqualTo(12));
    }

    [Test]
    public async Task NestsGroupUnderClassCommand()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start("tools cleanup run"), Is.EqualTo(13));
    }

    [Test]
    public async Task MethodNameCanBeAPath()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start("report daily"), Is.EqualTo(14));
    }

    [Test]
    public async Task CreatesImplicitGroup()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start("audit log show"), Is.EqualTo(15));
        starter.InstantiateCommands();
        var audit = starter.RootCommand.Subcommands.Single(command => command.Name == "audit");
        Assert.That(audit.Action, Is.Null);
        Assert.That(audit.Subcommands.Any(command => command.Name == "log"), Is.True);
    }

    [Test]
    public async Task ExplicitGroupAdoptsImplicitGroup()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start("zone area list"), Is.EqualTo(16));
        Assert.That(await starter.Start("zone info"), Is.EqualTo(17));
        Assert.That(starter.RootCommand.Subcommands.Count(command => command.Name == "zone"), Is.EqualTo(1));
    }

    [Test]
    public void DuplicatePathThrows()
    {
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Clear().Add(typeof(CyberInternauts.CmdStarter.Tests.Commands.Erroneous.MethodGroups.DuplicatePathA).Namespace!);

        Assert.Throws<InvalidAttributeException>(() => starter.InstantiateCommands());
    }

    private static Starter CreateStarter()
    {
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Clear().Add(typeof(AdminCommands).Namespace!);
        return starter;
    }
}
