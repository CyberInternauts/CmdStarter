using System.Reflection;
using CyberInternauts.CmdStarter.Catalog;

namespace CyberInternauts.CmdStarter.Tests;

// An assembly that exists only for a test: it is not loaded anywhere (not in the AppDomain) and carries the attributes it is given, so a catalog that cannot be created
// can be tested without loading an assembly. Without a catalog type, it has no catalog and is scanned through Types.
internal sealed class CatalogAssembly : Assembly
{
    private readonly string name;
    private readonly Attribute[] attributes;

    internal CatalogAssembly(string name, Type? catalogType)
    {
        this.name = name;
        attributes = catalogType is null ? [] : [new CommandCatalogAttribute(catalogType)];
    }

    // The assemblies it references: the library by default, because an assembly that carries a catalog references it.
    internal AssemblyName[] References { get; init; } = [typeof(Starter).Assembly.GetName()];

    // The types a scan finds in it.
    internal Type[] Types { get; init; } = [];

    // Thrown by GetReferencedAssemblies (Native AOT throws a PlatformNotSupportedException) and by the read of the attributes (a satellite assembly of a trimmed application does not load).
    internal Exception? ReferencesFailure { get; init; }

    internal Exception? AttributesFailure { get; init; }

    internal int ReferencedAssembliesCalls { get; private set; }

    internal int TypesCalls { get; private set; }

    internal int AttributeReads { get; private set; }

    public override string FullName => name;

    public override AssemblyName GetName() => new(name);

    public override AssemblyName[] GetReferencedAssemblies()
    {
        ReferencedAssembliesCalls++;
        return ReferencesFailure is null ? References : throw ReferencesFailure;
    }

    public override Type[] GetTypes()
    {
        TypesCalls++;
        return Types;
    }

    public override object[] GetCustomAttributes(bool inherit) => attributes;

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        AttributeReads++;
        return AttributesFailure is null ? attributes.Where(attributeType.IsInstanceOfType).ToArray() : throw AttributesFailure; // An Attribute[]: Attribute.GetCustomAttribute casts the array
    }

    public override bool IsDefined(Type attributeType, bool inherit) => attributes.Any(attributeType.IsInstanceOfType);
}

// The constructor is private: Activator cannot create it.
public sealed class PrivateConstructorCatalog : ICommandCatalog
{
    private PrivateConstructorCatalog()
    {
    }

    public void Describe(ICommandCatalogBuilder builder)
    {
    }
}

public sealed class ThrowingConstructorCatalog : ICommandCatalog
{
    public ThrowingConstructorCatalog() => throw new InvalidOperationException("the constructor failed");

    public void Describe(ICommandCatalogBuilder builder)
    {
    }
}

public sealed class ThrowingDescribeCatalog : ICommandCatalog
{
    public void Describe(ICommandCatalogBuilder builder) => throw new InvalidOperationException("the description failed");
}

// Out of memory is not a configuration error: the catalog reader lets it leave as it is.
public sealed class OutOfMemoryCatalog : ICommandCatalog
{
    public void Describe(ICommandCatalogBuilder builder) => throw new OutOfMemoryException("no memory left");
}

// A hand-written catalog that passes null where a type is expected.
public sealed class NullTypeCatalog : ICommandCatalog
{
    public void Describe(ICommandCatalogBuilder builder) => builder.AddMethodCommands(null!);
}
