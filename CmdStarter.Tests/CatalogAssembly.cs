using System.Reflection;
using CyberInternauts.CmdStarter.Catalog;

namespace CyberInternauts.CmdStarter.Tests;

// An assembly that exists only for a test: it is not loaded anywhere (not in the AppDomain) and carries the attributes it is given, so a catalog that cannot be created
// can be tested without loading an assembly.
internal sealed class CatalogAssembly : Assembly
{
    private readonly string name;
    private readonly Attribute[] attributes;

    internal CatalogAssembly(string name, Type catalogType)
    {
        this.name = name;
        attributes = [new CommandCatalogAttribute(catalogType)];
    }

    public override string FullName => name;

    public override AssemblyName GetName() => new(name);

    public override object[] GetCustomAttributes(bool inherit) => attributes;

    public override object[] GetCustomAttributes(Type attributeType, bool inherit) => attributes.Where(attributeType.IsInstanceOfType).ToArray(); // An Attribute[]: Attribute.GetCustomAttribute casts the array

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
