using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("Attributes")]
public class AliasAttributeConstructors
{
    [Test]
    public void ConstructorsRejectNullAliases()
    {
        var withoutPrefix = Assert.Throws<ArgumentNullException>(() => _ = new AliasAttribute((string[])null!));
        var withPrefix = Assert.Throws<ArgumentNullException>(() => _ = new AliasAttribute(true, null!));

        Assert.That(withoutPrefix!.ParamName, Is.EqualTo("aliases"));
        Assert.That(withPrefix!.ParamName, Is.EqualTo("aliases"));
    }
}
