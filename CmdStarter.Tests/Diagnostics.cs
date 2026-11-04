using CyberInternauts.CmdStarter.Exceptions;
using CyberInternauts.CmdStarter.Tests.Commands.Methods.Erroneous;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("CmdStarter")]
[Category("Diagnostics")]
public class Diagnostics
{
    [Test]
    public void ConfigurationExceptionsShareABaseType()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(CmdStarterException).IsAssignableFrom(typeof(InvalidAttributeException)), Is.True);
            Assert.That(typeof(CmdStarterException).IsAssignableFrom(typeof(NoCommandFoundException)), Is.True);
            Assert.That(typeof(CmdStarterException).IsAssignableFrom(typeof(LoopException)), Is.True);
        });
    }

    [Test]
    public void NoCommandFoundMessageNamesTheFilter()
    {
        var starter = TestsCommon.CreateCmdStarter();
        starter.Namespaces = starter.Namespaces.Add("com2.cyberint");

        var exception = Assert.Throws<NoCommandFoundException>(() => starter.FindCommandsTypes());

        Assert.That(exception!.Message, Does.Contain("Namespaces"));
    }

    [Test]
    public void InvalidMethodCommandMessageNamesTypeAndMethod()
    {
        var starter = new CmdStarter.Starter();
        starter.Namespaces = starter.Namespaces.Clear();
        starter.Classes = starter.Classes.Clear().Add(typeof(GenericMethodCommands).FullName!);

        var exception = Assert.Throws<InvalidAttributeException>(() => starter.InstantiateCommands());

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain(typeof(GenericMethodCommands).FullName!));
            Assert.That(exception.Message, Does.Contain("Run"));
        });
    }
}
