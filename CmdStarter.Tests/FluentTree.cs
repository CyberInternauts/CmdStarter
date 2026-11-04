using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Tests.Commands.FluentTree;
using CyberInternauts.CmdStarter.Loader;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("FluentTree")]
public class FluentTree
{
    private const string NamespaceName = "CyberInternauts.CmdStarter.Tests.Commands.FluentTree";

    [Test]
    public async Task TreeDefinesHierarchy()
    {
        var starter = CreateStarter();
        starter.UseTree(DefineCommonTree);

        Assert.That(await starter.Start("users list"), Is.EqualTo(31));
        Assert.That(await starter.Start("groups list"), Is.EqualTo(32));
        Assert.That(await starter.Start("status"), Is.EqualTo(33));
    }

    [Test]
    public void TreeIgnoresDiscoveryAndParentAttributes()
    {
        var starter = CreateStarter();
        starter.UseTree(DefineCommonTree);
        starter.InstantiateCommands();

        Assert.That(starter.FindCommand<Orphan>(), Is.Null);
        Assert.That(starter.RootCommand.Subcommands.Select(command => command.Name), Is.EqualTo(new[] { "users", "groups", "status" }));
    }

    [Test]
    public async Task TreeCombinesWithMethodCommands()
    {
        var starter = CreateStarter();
        starter.UseTree(DefineCommonTree);

        Assert.That(await starter.Start("users export"), Is.EqualTo(35));
    }

    [Test]
    public void AddingSameTypeTwiceThrows()
    {
        var starter = CreateStarter();

        Assert.Throws<ArgumentException>(() => starter.UseTree(tree => tree.Add<Status>().Add<Users>(users => users.Add<Status>())));
    }

    [Test]
    public void AddingNonCommandTypeThrows()
    {
        var starter = CreateStarter();

        Assert.Throws<ArgumentException>(() => starter.UseTree(tree => tree.Add(typeof(string))));
        Assert.Throws<ArgumentException>(() => starter.UseTree(tree => tree.Add(typeof(IStarterCommand))));
    }

    [Test]
    public async Task FailedDefinitionKeepsPreviousTree()
    {
        var starter = CreateStarter();
        starter.UseTree(DefineCommonTree);

        Assert.Throws<InvalidOperationException>(() => starter.UseTree(_ => throw new InvalidOperationException("failed")));
        Assert.That(await starter.Start("status"), Is.EqualTo(33));
    }

    [Test]
    public void CapturedDefinitionIsClosed()
    {
        var starter = CreateStarter();
        CommandTreeDefinition? captured = null;
        starter.UseTree(tree => captured = tree);

        Assert.Throws<InvalidOperationException>(() => captured!.Add<Status>());
    }

    [Test]
    public async Task LonelyTreeCommandIsRooted()
    {
        var starter = new Starter(new[] { NamespaceName, TestsCommon.EXCLUSION_SYMBOL + NamespaceName + ".Methods" });
        starter.UseTree(tree => tree.Add<Status>());

        Assert.That(await starter.Start(Array.Empty<string>()), Is.EqualTo(33));
    }

    [Test]
    public async Task InterfaceCommandInTree()
    {
        var starter = CreateStarter();
        starter.UseTree(tree => tree.Add<InterfaceLeaf>().Add<Status>());

        Assert.That(await starter.Start("interface-leaf"), Is.EqualTo(36));
    }

    [Test]
    public async Task BuilderUseTreeConfiguresStarter()
    {
        await using var app = new CmdStarterBuilder()
            .ConfigureStarter(starter => starter.Namespaces = ImmutableList.Create(NamespaceName))
            .UseTree(DefineCommonTree)
            .Build();

        Assert.That(await app.RunAsync("status"), Is.EqualTo(33));
    }

    private static Starter CreateStarter() => new(new[] { NamespaceName });

    private static void DefineCommonTree(CommandTreeDefinition tree)
    {
        tree.Add<Users>(users => users.Add<ListUsers>())
            .Add<Groups>(groups => groups.Add<ListGroups>())
            .Add<Status>();
    }
}
