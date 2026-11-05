using CyberInternauts.CmdStarter.Tests.Commands.States;
using System.Collections.Immutable;
using Erroneous = CyberInternauts.CmdStarter.Tests.Commands.Erroneous;

namespace CyberInternauts.CmdStarter.Tests;

/// <summary>
/// A construction step that fails leaves nothing half done: the next call runs the step again and fails the same way.
/// </summary>
[Category("* All *")]
[Category("CmdStarter")]
[Category("FailedConstruction")]
public class FailedConstruction
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [Test]
    public async Task FailedDiscoveryThrowsAgainOnNextStart()
    {
        var starter = new Starter
        {
            Namespaces = ["com2.cyberint"],
            StateProvider = new TestStateProvider { CurrentState = TestState.Anonymous }
        };

        var first = Assert.ThrowsAsync<NoCommandFoundException>(() => starter.Start("always"));
        var second = Assert.ThrowsAsync<NoCommandFoundException>(() => starter.Start("always"));

        Assert.That(second!.LastFilterApplied, Is.EqualTo(first!.LastFilterApplied));
        starter.Namespaces = [typeof(Enter).Namespace!];
        Assert.That(await starter.Start("always"), Is.EqualTo(30));
    }

    [Test]
    public async Task FailedInstantiationThrowsAgainOnNextStart()
    {
        var starter = new Starter { Namespaces = [typeof(Enter).Namespace!] };

        var first = Assert.ThrowsAsync<InvalidAttributeException>(() => starter.Start("always"));
        var second = Assert.ThrowsAsync<InvalidAttributeException>(() => starter.Start("always"));

        Assert.That(second!.Message, Is.EqualTo(first!.Message));
        starter.StateProvider = new TestStateProvider { CurrentState = TestState.Anonymous };
        Assert.That(await starter.Start("always"), Is.EqualTo(30));
    }

    [Test]
    public void FailedSynchronousInstantiationThrowsAgainOnNextCall()
    {
        var starter = new Starter { Namespaces = [typeof(Enter).Namespace!] };

        var first = Assert.Throws<InvalidAttributeException>(() => starter.InstantiateCommands());
        var second = Assert.Throws<InvalidAttributeException>(() => starter.InstantiateCommands());

        Assert.That(second!.Message, Is.EqualTo(first!.Message));
        starter.StateProvider = new TestStateProvider { CurrentState = TestState.Anonymous };
        Assert.DoesNotThrow(() => starter.InstantiateCommands());
        Assert.That(starter.FindCommand<Enter>(), Is.Not.Null);
    }

    [Test]
    public void FailedTreeBuildingThrowsAgainOnNextBuild()
    {
        var starter = new Starter
        {
            Namespaces = ImmutableList.Create(
                typeof(Erroneous.LoopChildren.Parent.LoopParent).Namespace!,
                typeof(Erroneous.LoopChildren.Child.LoopChild).Namespace!)
        };

        Assert.Throws<LoopException>(starter.BuildTree);
        Assert.Throws<LoopException>(starter.BuildTree);
    }
}
