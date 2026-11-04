using System.Collections.Immutable;
using System.Reflection;
using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter.Catalog;
using CyberInternauts.CmdStarter.Exceptions;
using CyberInternauts.CmdStarter.Interfaces;
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

    [Test]
    public void GeneratedCatalogMatchesTheScan()
    {
        // CmdStarter.Tests.Commands carries the catalog that the generator wrote. No other filter: the Erroneous namespaces are there, and nothing is instantiated.
        var assembly = typeof(Commands.Remote.Hello).Assembly;
        Assert.That(assembly.GetCustomAttribute<CommandCatalogAttribute>(), Is.Not.Null, "the assembly has a generated catalog");
        var catalog = new Starter { Assemblies = ImmutableList.Create(assembly) };
        var scan = new Starter { Assemblies = ImmutableList.Create(assembly), UseCommandCatalogs = false };

        catalog.FindCommandsTypes();
        scan.FindCommandsTypes();

        Assert.That(catalog.CommandsTypes, Is.Not.Empty);
        Assert.That(catalog.CommandsTypes, Is.EqualTo(scan.CommandsTypes));
        Assert.That(catalog.MethodCommandsTypes, Is.Not.Empty);
        Assert.That(catalog.MethodCommandsTypes, Is.EqualTo(scan.MethodCommandsTypes));
    }

    [Test]
    public void GeneratedCatalogListsWhatTheScanFinds()
    {
        // The three lists, through the public API: the catalog is created from its attribute and described to a recorder. GlobalOptionsManager is not used,
        // because it keeps what it found for the whole process.
        var assembly = typeof(Commands.Remote.Hello).Assembly;
        var recorder = new RecordingBuilder();
        ((ICommandCatalog)Activator.CreateInstance(assembly.GetCustomAttribute<CommandCatalogAttribute>()!.CatalogType)!).Describe(recorder);

        // The predicates of the scan, written again here.
        var types = assembly.GetTypes();
        var commands = types.Where(type => type.IsClass && !type.IsAbstract && type.IsAssignableTo(typeof(IStarterCommand)) && type.Namespace != null);
        var methodClasses = types.Where(type => type.IsClass && type.Namespace != null && (Attribute.IsDefined(type, typeof(CommandAttribute))
            || type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly)
                .Any(method => Attribute.IsDefined(method, typeof(CommandAttribute)))));
        var containers = types.Where(type => type.IsClass && type.IsAssignableTo(typeof(IGlobalOptionsContainer)));

        Assert.That(recorder.Commands, Is.Not.Empty);
        Assert.That(recorder.Commands, Is.EquivalentTo(commands));
        Assert.That(recorder.MethodClasses, Is.Not.Empty);
        Assert.That(recorder.MethodClasses, Is.EquivalentTo(methodClasses));
        Assert.That(recorder.GlobalOptions, Is.Not.Empty);
        Assert.That(recorder.GlobalOptions, Is.EquivalentTo(containers));
    }

    private static Starter CreateStarter() => new() { Assemblies = ImmutableList.Create(typeof(Listed).Assembly) };

    // Records what a catalog declares.
    private sealed class RecordingBuilder : ICommandCatalogBuilder
    {
        internal List<Type> Commands { get; } = new();

        internal List<Type> MethodClasses { get; } = new();

        internal List<Type> GlobalOptions { get; } = new();

        public void AddCommand<TCommand>() where TCommand : class, IStarterCommand => Commands.Add(typeof(TCommand));

        public void AddMethodCommands(Type declaringType) => MethodClasses.Add(declaringType);

        public void AddGlobalOptions<TContainer>() where TContainer : class, IGlobalOptionsContainer => GlobalOptions.Add(typeof(TContainer));
    }
}
