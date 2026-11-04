using CyberInternauts.CmdStarter.Tests.Commands.Validation;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("Validation")]
public sealed class ValidationAttributes
{
    [Test]
    public async Task RangeAcceptsValueInside()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start("level-command --level 7"), Is.EqualTo(7));
    }

    [Test]
    public async Task RangeRejectsValueOutside()
    {
        var starter = CreateStarter();

        var result = await starter.Start("level-command --level 11");

        Assert.That(result, Is.Not.Zero.And.Not.EqualTo(11));
    }

    [Test]
    public async Task RequiredOptionIsEnforced()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start("named-command"), Is.Not.Zero);
        Assert.That(await starter.Start("named-command --name ann"), Is.Zero);
    }

    [Test]
    public async Task RequiredRejectsEmptyString()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start("named-command --name \"\""), Is.Not.Zero);
    }

    [Test]
    public async Task ClassHandlerArgumentIsValidated()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start("code-command abc"), Is.Zero);
        Assert.That(await starter.Start("code-command ABC"), Is.Not.Zero);
    }

    [Test]
    public async Task MethodArgumentIsValidated()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start("pick red"), Is.EqualTo(1));
        Assert.That(await starter.Start("pick green"), Is.Not.Zero);
    }

    [Test]
    public async Task MethodOptionIsValidated()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start("pick blue --size 3"), Is.EqualTo(3));
        Assert.That(await starter.Start("pick blue --size 5"), Is.Not.Zero);
    }

    [Test]
    public async Task GlobalOptionIsValidated()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start("pick red --verbosity 4"), Is.Not.Zero);
        Assert.That(await starter.Start("pick red --verbosity 2"), Is.EqualTo(1));
    }

    [Test]
    public async Task UnparsableOptionValueIsAParseError()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start("level-command --level abc"), Is.Not.Zero.And.Not.EqualTo(5));
    }

    [Test]
    public async Task UnparsableArgumentValueIsAParseError()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start("slot-command abc"), Is.Not.Zero);
        Assert.That(await starter.Start("slot-command 4"), Is.EqualTo(4));
        Assert.That(await starter.Start("slot-command 12"), Is.Not.Zero.And.Not.EqualTo(12));
    }

    private static Starter CreateStarter()
    {
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Clear().Add(typeof(LevelCommand).Namespace!);
        return starter;
    }
}
