using CyberInternauts.CmdStarter.Tests.Commands.SameLeafNames;
using CyberInternauts.CmdStarter.Loader;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("NameConflicts")]
public sealed class NameConflicts
{
    [Test]
    public void SiblingAliasConflictThrows()
    {
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Add(typeof(CyberInternauts.CmdStarter.Tests.Commands.Erroneous.NameConflicts.AliasVsName.Alpha).Namespace!);

        var exception = Assert.Throws<InvalidAttributeException>(() => starter.InstantiateCommands());
        Assert.That(exception!.Message, Does.Contain("'alpha'"));
    }

    [Test]
    public void ChildNamedLikeParentThrows()
    {
        var starter = new Starter { ClassesBuildingMode = ClassesBuildingMode.OnlyAttributes };
        starter.Namespaces = starter.Namespaces.Add(typeof(CyberInternauts.CmdStarter.Tests.Commands.Erroneous.NameConflicts.ChildLikeParent.Get).Namespace!);

        var exception = Assert.Throws<InvalidAttributeException>(() => starter.InstantiateCommands());
        Assert.That(exception!.Message, Does.Contain("'get'"));
    }

    [Test]
    public void DuplicateOptionAliasThrows()
    {
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Add(typeof(CyberInternauts.CmdStarter.Tests.Commands.Erroneous.NameConflicts.DuplicateOptionAlias.TwoOptions).Namespace!);

        var exception = Assert.Throws<InvalidAttributeException>(() => starter.InstantiateCommands());
        Assert.That(exception!.Message, Does.Contain("'-x'"));
    }

    [Test]
    public void LonelyOptionConflictingWithHelpThrows()
    {
        var starter = new Starter { IsRootingLonelyCommand = true };
        starter.Namespaces = starter.Namespaces.Add(typeof(CyberInternauts.CmdStarter.Tests.Commands.Erroneous.NameConflicts.HelpAlias.HostCommand).Namespace!);

        var exception = Assert.Throws<InvalidAttributeException>(() => starter.InstantiateCommands());
        Assert.That(exception!.Message, Does.Contain("'-h'"));
    }

    [Test]
    public void MethodAliasConflictingWithClassCommandThrows()
    {
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Add(typeof(CyberInternauts.CmdStarter.Tests.Commands.Erroneous.NameConflicts.MethodAlias.Pong).Namespace!);

        var exception = Assert.Throws<InvalidAttributeException>(() => starter.InstantiateCommands());
        Assert.That(exception!.Message, Does.Contain("'pong'"));
    }

    [Test]
    public async Task SameLeafNameUnderDifferentParentsRuns()
    {
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Add(typeof(Users).Namespace!);

        Assert.DoesNotThrow(starter.InstantiateCommands);
        Assert.That(await starter.Start("users list"), Is.EqualTo(21));
        Assert.That(await starter.Start("groups list"), Is.EqualTo(22));
    }
}
