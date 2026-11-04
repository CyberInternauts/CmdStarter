using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Catalog;
using CyberInternauts.CmdStarter.Exceptions;
using CyberInternauts.CmdStarter.Tests.Commands.Catalog;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("Catalog")]
public class CommandCatalogs
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [Test]
    public void CatalogIsReadInsteadOfTheScan()
    {
        var starter = CreateStarter();

        starter.FindCommandsTypes();

        // Unlisted is a command that the scan finds; the catalog does not declare it.
        Assert.That(starter.CommandsTypes, Is.EqualTo(new[] { typeof(Listed) }));
    }

    [Test]
    public void ScanIsUsedWhenCatalogsAreOff()
    {
        var starter = CreateStarter();
        starter.UseCommandCatalogs = false;

        starter.FindCommandsTypes();

        Assert.That(starter.CommandsTypes, Is.EqualTo(new[] { typeof(Listed), typeof(Unlisted) }));
    }

    [Test]
    public void TurningCatalogsOffResetsDiscovery()
    {
        var starter = CreateStarter();
        starter.FindCommandsTypes();
        Assert.That(starter.CommandsTypes, Has.Count.EqualTo(1));

        starter.UseCommandCatalogs = false;
        Assert.That(starter.CommandsTypes, Is.Empty, "the discovery was reset");

        starter.FindCommandsTypes();
        Assert.That(starter.CommandsTypes, Is.EqualTo(new[] { typeof(Listed), typeof(Unlisted) }));
    }

    [Test]
    public void CatalogWithoutCommandsFindsNothing()
    {
        // The catalog is read, then the filter of the namespaces leaves nothing.
        var starter = CreateStarter();
        starter.Namespaces = ImmutableList.Create(typeof(Listed).Namespace + ".Empty");

        var exception = Assert.Throws<NoCommandFoundException>(() => starter.FindCommandsTypes());

        Assert.That(exception!.LastFilterApplied, Is.EqualTo(NoCommandFoundException.Filter.Namespaces));
    }

    [Test]
    public void CatalogCannotAddATypeTheScanWouldNotFind()
    {
        // The hand-written catalog declares AbstractListed, which is abstract: only Listed is found.
        var starter = CreateStarter();

        starter.FindCommandsTypes();

        Assert.That(starter.CommandsTypes, Does.Not.Contain(typeof(AbstractListed)));
    }

    [Test]
    public void CatalogAttributeRefusesANullType()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new CommandCatalogAttribute(null!));
    }

    [TestCase(typeof(PrivateConstructorCatalog), "")] // The reason is the message of the runtime, which is localized: only the names are checked.
    [TestCase(typeof(ThrowingConstructorCatalog), "the constructor failed")]
    [TestCase(typeof(ThrowingDescribeCatalog), "the description failed")]
    [TestCase(typeof(NullTypeCatalog), "declaringType")]
    public void CatalogThatFailsIsAConfigurationError(Type catalogType, string reason)
    {
        var starter = new Starter { Assemblies = ImmutableList.Create<System.Reflection.Assembly>(new CatalogAssembly("TestCatalogAssembly", catalogType)) };

        var exception = Assert.Throws<InvalidAttributeException>(() => starter.FindCommandsTypes());

        Assert.That(exception!.Message, Does.Contain("TestCatalogAssembly"));
        Assert.That(exception.Message, Does.Contain(catalogType.FullName));
        Assert.That(exception.Message, Does.Contain(reason));

        // A failed discovery leaves nothing half done: the next call fails the same way.
        Assert.Throws<InvalidAttributeException>(() => starter.FindCommandsTypes());
    }

    [Test]
    public void OutOfMemoryOfACatalogIsNotWrapped()
    {
        var starter = new Starter { Assemblies = ImmutableList.Create<System.Reflection.Assembly>(new CatalogAssembly("TestCatalogAssembly", typeof(OutOfMemoryCatalog))) };

        var exception = Assert.Throws<OutOfMemoryException>(() => starter.FindCommandsTypes());

        Assert.That(exception!.Message, Is.EqualTo("no memory left"));
    }

    private static Starter CreateStarter() => new() { Assemblies = ImmutableList.Create(typeof(Listed).Assembly) };
}
