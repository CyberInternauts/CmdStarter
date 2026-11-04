using System.Data;
using CyberInternauts.CmdStarter.Loader;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("Loader")]
public class TreeNodes
{
    [Test]
    public void AddedChildrenKeepTheirOrderAndKnowTheirParent()
    {
        var root = new TreeNode<string>(null);

        var first = root.AddChild("first");
        var added = root.AddChildren("second", "third");

        Assert.That(root.Value, Is.Null);
        Assert.That(root.Children, Is.EqualTo(new[] { first, added[0], added[1] }));
        Assert.That(root[0], Is.SameAs(first));
        Assert.That(root[2].Value, Is.EqualTo("third"));
        Assert.That(added.Select(node => node.Value), Is.EqualTo(new[] { "second", "third" }));
        Assert.That(root.Children.Select(node => node.Parent), Is.All.SameAs(root));
        Assert.That(root.Parent, Is.Null);
    }

    [Test]
    public void AddedNodeIsAdoptedWithItsOwnChildren()
    {
        var root = new TreeNode<string>("root");
        var branch = new TreeNode<string>("branch");
        branch.AddChild("leaf");

        var returned = root.AddChild(branch);

        Assert.That(returned, Is.SameAs(branch));
        Assert.That(branch.Parent, Is.SameAs(root));
        Assert.That(root.FlattenValues(), Is.EqualTo(new[] { "root", "branch", "leaf" }));
    }

    [Test]
    public void TraverseVisitsEveryValueDepthFirstAndSkipsEmptyNodes()
    {
        var root = new TreeNode<string>(null);
        var a = root.AddChild("a");
        a.AddChild("a1");
        root.AddChild("b");
        var visited = new List<string>();

        root.Traverse(visited.Add);

        Assert.That(visited, Is.EqualTo(new[] { "a", "a1", "b" }));
    }

    [Test]
    public void FlattenListsTheNodeThenItsDescendants()
    {
        var root = new TreeNode<string>(null);
        var a = root.AddChild("a");
        var a1 = a.AddChild("a1");
        var b = root.AddChild("b");

        Assert.That(root.FlattenNodes(), Is.EqualTo(new[] { root, a, a1, b }));
        Assert.That(root.FlattenValues(), Is.EqualTo(new[] { "a", "a1", "b" }));
        Assert.That(a.FlattenNodes(), Is.EqualTo(new[] { a, a1 }));
    }

    [Test]
    public void FindNodeLooksAtTheNodeThenItsDescendants()
    {
        var root = new TreeNode<string>("root");
        var deep = root.AddChild("a").AddChild("deep");

        Assert.That(root.FindNode("root"), Is.SameAs(root));
        Assert.That(root.FindNode("deep"), Is.SameAs(deep));
        Assert.That(root.FindNode("absent"), Is.Null);
        Assert.That(deep.FindNode("root"), Is.Null);
        Assert.That(root.FindNode(null!), Is.Null);
    }

    [Test]
    public void RootNodeIsTheTopOfTheBranch()
    {
        var root = new TreeNode<string>("root");
        var deep = root.AddChild("a").AddChild("deep");

        Assert.That(deep.GetRootNode(), Is.SameAs(root));
        Assert.That(root.GetRootNode(), Is.SameAs(root));
    }

    [Test]
    public void RemoveChildTellsWhetherTheNodeWasAChild()
    {
        var root = new TreeNode<string>("root");
        var child = root.AddChild("child");
        var stranger = new TreeNode<string>("stranger");

        Assert.That(root.RemoveChild(stranger), Is.False);
        Assert.That(root.RemoveChild(child), Is.True);
        Assert.That(root.Children, Is.Empty);
        Assert.That(root.RemoveChild(child), Is.False);
    }

    [Test]
    public void MovedNodeLeavesItsParentAndKeepsItsDescendants()
    {
        var root = new TreeNode<string>("root");
        var from = root.AddChild("from");
        var to = root.AddChild("to");
        var moved = from.AddChild("moved");
        moved.AddChild("leaf");

        moved.MoveNode(to);

        Assert.That(from.Children, Is.Empty);
        Assert.That(to.Children, Is.EqualTo(new[] { moved }));
        Assert.That(moved.Parent, Is.SameAs(to));
        Assert.That(to.FlattenValues(), Is.EqualTo(new[] { "to", "moved", "leaf" }));
    }

    [Test]
    public void NodeWithoutParentCanBeMoved()
    {
        var loose = new TreeNode<string>("loose");
        var target = new TreeNode<string>("target");

        loose.MoveNode(target);

        Assert.That(loose.Parent, Is.SameAs(target));
        Assert.That(target.Children, Is.EqualTo(new[] { loose }));
    }

    [Test]
    public void LockingOneNodeLocksTheWholeTreeAgainstEveryModification()
    {
        var root = new TreeNode<string>("root");
        var child = root.AddChild("child");
        var leaf = child.AddChild("leaf");
        var other = new TreeNode<string>("other");

        leaf.SetImmutable();

        Assert.That(new[] { root, child, leaf }.Select(node => node.IsImmutable), Is.All.True);
        Assert.That(other.IsImmutable, Is.False);
        foreach (var node in new[] { root, child, leaf })
        {
            Assert.Throws<ReadOnlyException>(() => node.AddChild("added"));
            Assert.Throws<ReadOnlyException>(() => node.AddChild(new TreeNode<string>("added")));
            Assert.Throws<ReadOnlyException>(() => node.AddChildren("added", "other"));
            Assert.Throws<ReadOnlyException>(() => node.RemoveChild(leaf));
            Assert.Throws<ReadOnlyException>(() => node.MoveNode(other));
        }

        // Reading is unaffected, and nothing changed.
        Assert.That(root.FlattenValues(), Is.EqualTo(new[] { "root", "child", "leaf" }));
        Assert.That(other.Children, Is.Empty);
    }
}
