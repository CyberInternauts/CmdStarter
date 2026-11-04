using System.Collections.Immutable;
using System.Reflection;
using CyberInternauts.CmdStarter.Catalog;
using CyberInternauts.CmdStarter.Exceptions;
using CyberInternauts.CmdStarter.Loader;
using CyberInternauts.CmdStarter.Tests.Commands.Catalog;
using CyberInternauts.CmdStarter.Tests.Commands.GlobalOptions;
using CyberInternauts.CmdStarter.Tests.Commands.Methods;

namespace CyberInternauts.CmdStarter.Tests;

// The two ways to find the commands: with dynamic code (CoreCLR), an assembly is read from its catalog or scanned, and with none (Native AOT) only the catalogs are read.
// The tests use assemblies made for them, which count the calls, and the setting that tells whether the runtime supports dynamic code.
[Category("* All *")]
[Category("Catalog")]
public class DynamicCodeSupport
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [Test]
    public void AttributeIsReadOnlyOnAssembliesThatReferenceTheLibrary()
    {
        // This assembly carries a catalog but does not reference the library: its attributes are not read, and it is scanned. Its references are asked once.
        var unrelated = new CatalogAssembly("Unrelated", typeof(CatalogWithContainer)) { References = [new AssemblyName("Elsewhere")], Types = [typeof(Unlisted), typeof(SecondaryGlobalOptions)] };
        var starter = new Starter { Assemblies = ImmutableList.Create<Assembly>(unrelated) };

        starter.FindCommandsTypes();

        Assert.That(starter.CommandsTypes, Is.EqualTo(new[] { typeof(Unlisted) }), "scanned, and the catalog it carries is not read");
        Assert.That(unrelated.AttributeReads, Is.EqualTo(0));
        Assert.That(unrelated.ReferencedAssembliesCalls, Is.EqualTo(1));
        Assert.That(GlobalOptionsManager.FindContainerTypes([unrelated], useCatalogs: true, dynamicCodeSupported: true), Is.EqualTo(new[] { typeof(SecondaryGlobalOptions) }));

        // The same assembly with the library among its references: the catalog is read, and the references are asked once.
        var related = new CatalogAssembly("Related", typeof(CatalogWithContainer)) { Types = [typeof(Unlisted), typeof(SecondaryGlobalOptions)] };
        var other = new Starter { Assemblies = ImmutableList.Create<Assembly>(related) };

        other.FindCommandsTypes();

        Assert.That(other.CommandsTypes, Is.EqualTo(new[] { typeof(Listed) }), "the catalog is read, the scan is skipped");
        Assert.That(related.TypesCalls, Is.EqualTo(0));
        Assert.That(related.ReferencedAssembliesCalls, Is.EqualTo(1));
        Assert.That(GlobalOptionsManager.FindContainerTypes([related], useCatalogs: true, dynamicCodeSupported: true), Is.EqualTo(new[] { typeof(MainGlobalOptions) }));
    }

    [Test]
    public void WithoutDynamicCodeOnlyCatalogsAreRead()
    {
        // GetReferencedAssemblies throws on both, as on Native AOT: it must never be called. The plain assembly has no catalog and is not scanned.
        var notSupported = new PlatformNotSupportedException("Operation is not supported on this platform.");
        var catalog = new CatalogAssembly("WithCatalog", typeof(CatalogWithContainer)) { ReferencesFailure = notSupported };
        var plain = new CatalogAssembly("Plain", catalogType: null) { ReferencesFailure = notSupported, Types = [typeof(Unlisted), typeof(SecondaryGlobalOptions)] };
        var starter = new Starter { DynamicCodeSupported = false, Assemblies = ImmutableList.Create<Assembly>(catalog, plain) };

        starter.FindCommandsTypes();

        Assert.That(starter.CommandsTypes, Is.EqualTo(new[] { typeof(Listed) }));
        Assert.That(GlobalOptionsManager.FindContainerTypes([catalog, plain], useCatalogs: true, dynamicCodeSupported: false), Is.EqualTo(new[] { typeof(MainGlobalOptions) }));
        Assert.That(catalog.ReferencedAssembliesCalls + plain.ReferencedAssembliesCalls, Is.EqualTo(0));
        Assert.That(plain.TypesCalls, Is.EqualTo(0));
    }

    [Test]
    public void WithoutDynamicCodeAnAssemblyWithoutCatalogFindsNoCommand()
    {
        // Nothing can be found: the search says so, as it does when no assembly has a command.
        var plain = new CatalogAssembly("Plain", catalogType: null) { Types = [typeof(Unlisted)] };
        var starter = new Starter { DynamicCodeSupported = false, Assemblies = ImmutableList.Create<Assembly>(plain) };

        var exception = Assert.Throws<NoCommandFoundException>(() => starter.FindCommandsTypes());

        Assert.That(exception!.LastFilterApplied, Is.EqualTo(NoCommandFoundException.Filter.Assemblies));
        Assert.That(plain.TypesCalls, Is.EqualTo(0));
    }

    [TestCase(true, typeof(FileNotFoundException))]
    [TestCase(true, typeof(FileLoadException))]
    [TestCase(true, typeof(TypeLoadException))]
    [TestCase(false, typeof(FileNotFoundException))]
    [TestCase(false, typeof(FileLoadException))]
    [TestCase(false, typeof(TypeLoadException))]
    public void AnAssemblyWhoseAttributesCannotBeReadHasNoCatalog(bool dynamicCodeSupported, Type failureType)
    {
        // With dynamic code, an assembly without catalog is scanned; without, it is left alone. The assembly with a catalog is read in both cases.
        var failure = (Exception)Activator.CreateInstance(failureType, "The attributes cannot be read.")!;
        var broken = new CatalogAssembly("Broken", typeof(CatalogWithContainer)) { AttributesFailure = failure, Types = [typeof(Unlisted), typeof(SecondaryGlobalOptions)] };
        var healthy = new CatalogAssembly("Healthy", typeof(CatalogWithContainer));
        var starter = new Starter { DynamicCodeSupported = dynamicCodeSupported, Assemblies = ImmutableList.Create<Assembly>(broken, healthy) };

        starter.FindCommandsTypes();
        var containers = GlobalOptionsManager.FindContainerTypes([broken, healthy], useCatalogs: true, dynamicCodeSupported);

        if (dynamicCodeSupported)
        {
            Assert.That(starter.CommandsTypes, Is.EqualTo(new[] { typeof(Listed), typeof(Unlisted) }));
            Assert.That(containers, Is.EquivalentTo(new[] { typeof(SecondaryGlobalOptions), typeof(MainGlobalOptions) }));
        }
        else
        {
            Assert.That(starter.CommandsTypes, Is.EqualTo(new[] { typeof(Listed) }));
            Assert.That(containers, Is.EqualTo(new[] { typeof(MainGlobalOptions) }));
            Assert.That(broken.TypesCalls, Is.EqualTo(0));
        }
    }

    [Test]
    public void WithCatalogsOffNothingIsReadNorAsked()
    {
        // The containers of an assembly are scanned and its attributes and references are left alone.
        var assembly = new CatalogAssembly("Scanned", typeof(CatalogWithContainer)) { Types = [typeof(Unlisted), typeof(SecondaryGlobalOptions)] };

        var containers = GlobalOptionsManager.FindContainerTypes([assembly], useCatalogs: false, dynamicCodeSupported: true);

        Assert.That(containers, Is.EqualTo(new[] { typeof(SecondaryGlobalOptions) }));
        Assert.That(assembly.AttributeReads + assembly.ReferencedAssembliesCalls, Is.EqualTo(0));
    }

    [Test]
    public void OtherFailuresOfTheReadOfAttributesAreNotHidden()
    {
        // Only the failures to load are tolerated: any other exception is a bug to report.
        var broken = new CatalogAssembly("Broken", typeof(CatalogWithContainer)) { AttributesFailure = new InvalidOperationException("another failure") };
        var starter = new Starter { Assemblies = ImmutableList.Create<Assembly>(broken) };

        var exception = Assert.Throws<InvalidOperationException>(() => starter.FindCommandsTypes());

        Assert.That(exception!.Message, Is.EqualTo("another failure"));
    }

    [Test]
    public void CommandMethodsKeepTheReflectionOrderWithoutDynamicCode()
    {
        // With dynamic code the methods are in the order of their declaration (the metadata token); without, in the order of GetMethods, which never reads the metadata token.
        // On CoreCLR the two orders coincide: the test shows that the two paths run and give the same methods.
        var type = typeof(ServiceMethodCommands);

        var declared = MethodCommandsLoader.GetCommandMethods(type, dynamicCodeSupported: true);
        var reflected = MethodCommandsLoader.GetCommandMethods(type, dynamicCodeSupported: false);

        Assert.That(declared, Has.Count.EqualTo(3));
        Assert.That(declared.Select(method => method.MetadataToken), Is.Ordered);
        Assert.That(reflected, Is.EquivalentTo(declared));
    }
}

// A catalog that declares a command and a global options container, for the assemblies made for the tests above.
public sealed class CatalogWithContainer : ICommandCatalog
{
    public void Describe(ICommandCatalogBuilder builder)
    {
        builder.AddCommand<Listed>();
        builder.AddGlobalOptions<MainGlobalOptions>();
    }
}
