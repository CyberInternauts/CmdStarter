using System.Collections;
using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter.Extensions;
using CyberInternauts.CmdStarter.Tests.Commands.ExecutionPaths;
using CyberInternauts.CmdStarter.Tests.Commands.FluentTree;
using CyberInternauts.CmdStarter.Tests.Commands.GlobalOptions;
using CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;

namespace CyberInternauts.CmdStarter.Tests;

/// <summary>Small public members of the library that no other test needs: collections, constructors and helpers.</summary>
[Category("* All *")]
[Category("PublicSurfaces")]
public class PublicSurfaces
{
    [Test]
    public void AliasAttributeIsACollectionOfItsAliases()
    {
        var attribute = new AliasAttribute("first", "second");

        Assert.That(attribute[0], Is.EqualTo("first"));
        Assert.That(attribute[1], Is.EqualTo("second"));
        Assert.That(attribute.ToArray(), Is.EqualTo(new[] { "first", "second" }));
        Assert.That(((IEnumerable)attribute).Cast<string>(), Is.EqualTo(new[] { "first", "second" }));
    }

    [Test]
    public void AliasPrefixIsAddedOnceWhateverTheNumberOfReads()
    {
        var defaultPrefix = new AliasAttribute(true, "x");
        var ownPrefix = new AliasAttribute(true, "y") { Prefix = "--" };

        Assert.That(defaultPrefix[0], Is.EqualTo("-x"));
        Assert.That(defaultPrefix.Aliases, Is.EqualTo(new[] { "-x" }));
        Assert.That(defaultPrefix.ToArray(), Is.EqualTo(new[] { "-x" }));
        Assert.That(ownPrefix[0], Is.EqualTo("--y"));
        Assert.That(ownPrefix.ToArray(), Is.EqualTo(new[] { "--y" }));
    }

    [Test]
    public void ChildrenAttributeTakesTheNamespaceOfTheTypeItIsGiven()
    {
        // A type of the global namespace has no namespace: the attribute names an empty one.
        Assert.That(new ChildrenAttribute(typeof(InterfaceLeaf)).Namespace, Is.EqualTo(typeof(InterfaceLeaf).Namespace));
        Assert.That(new ChildrenAttribute<InterfaceLeaf>().Namespace, Is.EqualTo(typeof(InterfaceLeaf).Namespace));
        Assert.That(typeof(GlobalNamespaceCommand).Namespace, Is.Null);
        Assert.That(new ChildrenAttribute(typeof(GlobalNamespaceCommand)).Namespace, Is.Empty);
        Assert.That(new ChildrenAttribute<GlobalNamespaceCommand>().Namespace, Is.Empty);
    }

    [Test]
    public void ChildrenAttributeConstructorsRejectNull()
    {
        var byName = Assert.Throws<ArgumentNullException>(() => _ = new ChildrenAttribute((string)null!));
        var byType = Assert.Throws<ArgumentNullException>(() => _ = new ChildrenAttribute((Type)null!));

        Assert.That(byName!.ParamName, Is.EqualTo("namespaceForChildren"));
        Assert.That(byType!.ParamName, Is.EqualTo("namespaceForChildren"));
    }

    [Test]
    public void ConfigurationExceptionsKeepTheirMessageAndTheirCause()
    {
        var cause = new InvalidOperationException("cause");

        var empty = new CmdStarterException();
        var withMessage = new CmdStarterException("message");
        var withCause = new CmdStarterException("message with cause", cause);
        var emptyAttribute = new InvalidAttributeException();
        var attributeWithMessage = new InvalidAttributeException("attribute message");

        Assert.That(empty.InnerException, Is.Null);
        Assert.That(withMessage.Message, Is.EqualTo("message"));
        Assert.That(withMessage.InnerException, Is.Null);
        Assert.That(withCause.Message, Is.EqualTo("message with cause"));
        Assert.That(withCause.InnerException, Is.SameAs(cause));
        Assert.That(emptyAttribute, Is.InstanceOf<CmdStarterException>());
        Assert.That(emptyAttribute.InnerException, Is.Null);
        Assert.That(attributeWithMessage.Message, Is.EqualTo("attribute message"));
    }

    [Test]
    public void PascalToKebabCaseGivesAnEmptyStringForNothing()
    {
        Assert.That(string.Empty.PascalToKebabCase(), Is.Empty);
        Assert.That(((string)null!).PascalToKebabCase(), Is.Empty);
        Assert.That("SomeCommand".PascalToKebabCase(), Is.EqualTo("some-command"));
    }

    [Test]
    public void CommandNameIsRequiredAndNormalized()
    {
        foreach (var invalid in new[] { null!, string.Empty, "   " })
        {
            var exception = Assert.Throws<ArgumentException>(() => _ = new NamedCommand(invalid));
            Assert.That(exception!.Message, Does.Contain("name"));
        }

        var named = new NamedCommand("MyCommand", "does things");
        Assert.That(named.Name, Is.EqualTo("my-command"));
        Assert.That(named.Description, Is.EqualTo("does things"));
        Assert.That(new NamedCommand("Already-Dashed").Name, Is.EqualTo("already-dashed"));
        Assert.That(new NamedCommand("plain").Description, Is.Null);
    }

    [Test]
    public void CommandOutsideATreeHasNoPathAndNoGlobalOptions()
    {
        var command = new NamedCommand("alone");

        Assert.That(command.GetFullCommandString(), Is.Empty);
        Assert.That(command.GetGlobalOptions<MainGlobalOptions>(), Is.Null);
        Assert.That(((IStarterCommand)command).GetGlobalOptions<MainGlobalOptions>(), Is.Null);
        Assert.That(new InterfaceLeaf().GetGlobalOptions<MainGlobalOptions>(), Is.Null);
    }

    [Test]
    public void GetInstanceCreatesANewCommandUnlessTheTypeDeclaresItsOwn()
    {
        var first = StarterCommand.GetInstance<InterfaceLeaf>();
        var second = StarterCommand.GetInstance<InterfaceLeaf>();
        var own = (SelfCreatedCommand)StarterCommand.GetInstance<SelfCreatedCommand>();

        Assert.That(first, Is.TypeOf<InterfaceLeaf>());
        Assert.That(second, Is.TypeOf<InterfaceLeaf>().And.Not.SameAs(first));
        Assert.That(own.Origin, Is.EqualTo("get-instance"));
    }

    [Test]
    public void DefaultGetInstanceOfTheInterfaceUsesTheParameterlessConstructor()
    {
        var first = CreateThroughTheInterface<InterfaceLeaf>();
        var second = CreateThroughTheInterface<InterfaceLeaf>();

        Assert.That(first, Is.TypeOf<InterfaceLeaf>());
        Assert.That(second, Is.TypeOf<InterfaceLeaf>().And.Not.SameAs(first));
        Assert.That(CreateThroughTheInterface<SelfCreatedCommand>(), Is.TypeOf<SelfCreatedCommand>().And.Property("Origin").EqualTo("get-instance"));
    }

    private static IStarterCommand CreateThroughTheInterface<TCommand>() where TCommand : IStarterCommand => TCommand.GetInstance<TCommand>();
}
