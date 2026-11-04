using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Tests.Commands.FluentTree;
using CyberInternauts.CmdStarter.Loader;
using Microsoft.Extensions.DependencyInjection;

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
    public async Task OptionsUseTreeAndFiltersApplyToMethodCommandsOnly()
    {
        // CmdStarterOptions.UseTree, through AddCmdStarter.
        var services = new ServiceCollection();
        services.AddCmdStarter(options => options
            .ConfigureStarter(starter => starter.Namespaces = ImmutableList.Create(NamespaceName, TestsCommon.EXCLUSION_SYMBOL + NamespaceName + ".Methods"))
            .UseTree(tree => tree.Add<Status>()));
        await using var provider = services.BuildServiceProvider();
        Assert.That(await provider.GetRequiredService<Starter>().Start(Array.Empty<string>()), Is.EqualTo(33));

        // The namespace filter does not apply to the classes of a defined tree...
        var elsewhere = new Starter(new[] { "Nothing.Here" });
        elsewhere.UseTree(tree => tree.Add<Status>());
        Assert.That(await elsewhere.Start(Array.Empty<string>()), Is.EqualTo(33));

        // ...but it still applies to method commands.
        Assert.That(await elsewhere.Start("users export"), Is.Not.Zero.And.Not.EqualTo(35));
        var included = CreateStarter();
        included.UseTree(tree => tree.Add<Status>());
        Assert.That(await included.Start("users export"), Is.EqualTo(35));
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

    [Test]
    public async Task GroupWithoutClassCreatesPath()
    {
        var starter = CreateStarter();
        starter.UseTree(tree => tree.Group("admin", admin => admin.Group("users", users => users.Add<ListUsers>())).Add<Status>());

        Assert.That(await starter.Start("admin users list"), Is.EqualTo(31));
        Assert.That(await starter.Start("admin"), Is.Not.EqualTo(0));
    }

    [Test]
    public async Task GroupUnderClassCommand()
    {
        var starter = CreateStarter();
        starter.UseTree(tree => tree.Add<Users>(users => users.Group("roles", roles => roles.Add<ListGroups>())));

        Assert.That(await starter.Start("users roles list"), Is.EqualTo(32));
    }

    [Test]
    public async Task GroupAliasesWork()
    {
        var starter = CreateStarter();
        starter.UseTree(tree => tree.Group("admin", admin => admin.WithAliases("adm").Add<Status>()));

        Assert.That(await starter.Start("adm status"), Is.EqualTo(33));
    }

    [Test]
    public async Task WithNameRenamesCommand()
    {
        var starter = CreateStarter();
        starter.UseTree(tree => tree.Add<Status>(status => status.WithName("state").WithAliases("st")).Add<Users>());

        Assert.That(await starter.Start("state"), Is.EqualTo(33));
        Assert.That(await starter.Start("st"), Is.EqualTo(33));
        Assert.That(starter.FindCommand<Status>()!.Name, Is.EqualTo("state"));
    }

    [Test]
    public async Task MethodCommandsJoinDefinedGroup()
    {
        var starter = CreateStarter();
        starter.UseTree(tree => tree.Group("users", users => users.Add<Status>()));

        Assert.That(await starter.Start("users export"), Is.EqualTo(35));
        Assert.That(await starter.Start("users status"), Is.EqualTo(33));
    }

    [Test]
    public void InvalidGroupDefinitionsThrow()
    {
        var starter = CreateStarter();
        Assert.Throws<ArgumentException>(() => starter.UseTree(tree => tree.Group(" ", _ => { })));
        Assert.Throws<ArgumentException>(() => starter.UseTree(tree => tree.Group("a b", _ => { })));
        Assert.Throws<ArgumentException>(() => starter.UseTree(tree => tree.Group("empty", _ => { })));
        Assert.Throws<InvalidOperationException>(() => starter.UseTree(tree => tree.WithName("x")));
        Assert.Throws<InvalidOperationException>(() => starter.UseTree(tree => tree.Group("group", group => group.WithName("x"))));
    }

    [Test]
    public async Task GroupedLonelyCommandIsNotRooted()
    {
        var starter = new Starter(new[] { NamespaceName, TestsCommon.EXCLUSION_SYMBOL + NamespaceName + ".Methods" });
        starter.UseTree(tree => tree.Group("admin", admin => admin.Add<Status>()));

        Assert.That(await starter.Start("admin status"), Is.EqualTo(33));
    }

    private static Starter CreateStarter() => new(new[] { NamespaceName });

    private static void DefineCommonTree(CommandTreeDefinition tree)
    {
        tree.Add<Users>(users => users.Add<ListUsers>())
            .Add<Groups>(groups => groups.Add<ListGroups>())
            .Add<Status>();
    }
}
