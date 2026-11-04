using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Loader;
using CyberInternauts.CmdStarter.Tests.Commands.TreeBuilding.ChildrenLoop.First;
using CyberInternauts.CmdStarter.Tests.Commands.TreeBuilding.ChildrenLoop.Second;
using CyberInternauts.CmdStarter.Tests.Commands.TreeBuilding.EmptyChildren;
using CyberInternauts.CmdStarter.Tests.Commands.TreeBuilding.EmptyChildren.Nested;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("Loader")]
public class TreeBuilding
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [Test]
    public void CommandsThatAreChildrenOfEachOtherThroughChildrenAttributesThrowALoop()
    {
        var starter = CreateStarter(typeof(LoopFirst).Namespace!, typeof(LoopSecond).Namespace!);

        var exception = Assert.Throws<LoopException>(starter.BuildTree);

        Assert.That(exception!.Command1, Is.EqualTo(typeof(LoopSecond)));
        Assert.That(exception.Command2, Is.EqualTo(typeof(LoopFirst)));
        Assert.That(exception.Message, Does.Contain(typeof(LoopFirst).FullName).And.Contain(typeof(LoopSecond).FullName));
    }

    [Test]
    public void ChildrenAttributeWithAnEmptyNamespaceNamesNoChildren()
    {
        var starter = CreateStarter(typeof(EmptyChildrenParent).Namespace!, typeof(EmptyChildrenSibling).Namespace!);
        starter.ClassesBuildingMode = ClassesBuildingMode.OnlyAttributes;

        starter.BuildTree();

        // Both commands stay at the top: an empty namespace did not make the sibling a child.
        var topLevel = starter.CommandsTypesTree.Children.Select(node => node.Value);
        Assert.That(topLevel, Is.EquivalentTo(new[] { typeof(EmptyChildrenParent), typeof(EmptyChildrenSibling) }));
        Assert.That(starter.CommandsTypesTree.Children.SelectMany(node => node.Children), Is.Empty);
    }

    private static Starter CreateStarter(params string[] namespaces)
    {
        var starter = TestsCommon.CreateCmdStarter();
        starter.Namespaces = ImmutableList.CreateRange(namespaces);
        return starter;
    }
}
