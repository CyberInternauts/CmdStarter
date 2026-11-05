using CyberInternauts.CmdStarter.Repl;
using CyberInternauts.CmdStarter.Tests.Commands.Repl;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
public class StarterConstructors
{
    [Test]
    public void StarterTakesACollectionExpression()
    {
        var starter = new Starter(["A.B", "C.D"]);

        Assert.That(starter.Namespaces, Is.EqualTo(new[] { "A.B", "C.D" }));
    }

    [Test]
    public void ReplStarterTakesACollectionExpression()
    {
        var starter = new ReplStarter(new ScriptedInputProvider(), ["A.B"]);

        Assert.That(starter.Namespaces, Is.EqualTo(new[] { "A.B" }));
    }

    [Test]
    public void StarterCopiesTheList()
    {
        var namespaces = new List<string> { "A.B" };
        var starter = new Starter(namespaces);

        namespaces.Add("C.D");

        Assert.That(starter.Namespaces, Is.EqualTo(new[] { "A.B" }));
    }

    [Test]
    public void ReplStarterCopiesTheList()
    {
        var namespaces = new List<string> { "A.B" };
        var starter = new ReplStarter(new ScriptedInputProvider(), namespaces);

        namespaces.Add("C.D");

        Assert.That(starter.Namespaces, Is.EqualTo(new[] { "A.B" }));
    }

    [Test]
    public void StarterTakesAnArray()
    {
        string[] namespaces = ["A.B", "C.D"];

        Assert.That(new Starter(namespaces).Namespaces, Is.EqualTo(namespaces));
        Assert.That(new ReplStarter(new ScriptedInputProvider(), namespaces).Namespaces, Is.EqualTo(namespaces));
    }
}
