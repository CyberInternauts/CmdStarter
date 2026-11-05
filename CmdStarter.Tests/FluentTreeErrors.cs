using CyberInternauts.CmdStarter.Loader;
using CyberInternauts.CmdStarter.Tests.Commands.FluentTree;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("FluentTree")]
public class FluentTreeErrors
{
    private const string NamespaceName = "CyberInternauts.CmdStarter.Tests.Commands.FluentTree";

    [Test]
    public void CommandWithChildrenRequiresTheChildrenDefinition()
    {
        var starter = CreateStarter();

        var byType = Assert.Throws<ArgumentNullException>(() => starter.UseTree(tree => tree.Add(typeof(Users), null!)));
        var generic = Assert.Throws<ArgumentNullException>(() => starter.UseTree(tree => tree.Add<Users>(null!)));

        Assert.That(byType!.ParamName, Is.EqualTo("children"));
        Assert.That(generic!.ParamName, Is.EqualTo("children"));
    }

    [Test]
    public void NullCommandTypeIsRefused()
    {
        var starter = CreateStarter();

        var exception = Assert.Throws<ArgumentNullException>(() => starter.UseTree(tree => tree.Add(null!)));

        Assert.That(exception!.ParamName, Is.EqualTo("commandType"));
    }

    [Test]
    public void RootOfTheTreeHasNoAliases()
    {
        var starter = CreateStarter();

        var exception = Assert.Throws<InvalidOperationException>(() => starter.UseTree(tree => tree.WithAliases("root-alias")));

        Assert.That(exception!.Message, Does.Contain("root"));
    }

    [Test]
    public void AliasesMustBeNonEmptySingleSegments()
    {
        var starter = CreateStarter();

        foreach (var invalid in new[] { string.Empty, "  ", "two words" })
        {
            var exception = Assert.Throws<ArgumentException>(() => starter.UseTree(tree => tree.Add<Status>(status => status.WithAliases("fine", invalid))));
            Assert.That(exception!.ParamName, Is.EqualTo("aliases"), invalid);
        }

        Assert.Throws<ArgumentException>(() => starter.UseTree(tree => tree.Add<Status>(status => status.WithAliases("fine", null!))));
        Assert.Throws<ArgumentNullException>(() => starter.UseTree(tree => tree.Add<Status>(status => status.WithAliases(null!))));
    }

    [Test]
    public void ClosedDefinitionRefusesEveryKindOfChange()
    {
        var starter = CreateStarter();
        CommandTreeDefinition? root = null;
        CommandTreeDefinition? command = null;
        CommandTreeDefinition? group = null;
        starter.UseTree(tree =>
        {
            root = tree;
            tree.Add<Status>(status => command = status);
            tree.Group("admin", admin => group = admin.Add<Users>());
        });

        Assert.Throws<InvalidOperationException>(() => root!.Group("late", late => late.Add<Groups>()));
        Assert.Throws<InvalidOperationException>(() => command!.WithName("late"));
        Assert.Throws<InvalidOperationException>(() => command!.WithAliases("late"));
        Assert.Throws<InvalidOperationException>(() => group!.WithAliases("late"));
    }

    private static Starter CreateStarter() => new(new[] { NamespaceName });
}
