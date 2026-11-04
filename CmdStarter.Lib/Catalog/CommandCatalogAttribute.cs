using System.Diagnostics.CodeAnalysis;

namespace CyberInternauts.CmdStarter.Catalog;

/// <summary>
/// Marks an assembly with the <see cref="ICommandCatalog"/> that lists the commands it contains. When a <see cref="Starter"/> searches an assembly that has this attribute,
/// it reads the catalog instead of scanning the types of the assembly by reflection (see <see cref="Starter.UseCommandCatalogs"/>).
/// The generator of the CmdStarter package writes this attribute and its catalog at compile time. An application can also write both by hand: a catalog declared this way
/// replaces the generated one.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class CommandCatalogAttribute : Attribute
{
    /// <summary>Creates the attribute.</summary>
    /// <param name="catalogType">The type of the catalog. It must implement <see cref="ICommandCatalog"/> and have a public parameterless constructor.</param>
    /// <exception cref="ArgumentNullException"><paramref name="catalogType"/> is null.</exception>
    public CommandCatalogAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type catalogType)
    {
        ArgumentNullException.ThrowIfNull(catalogType);
        CatalogType = catalogType;
    }

    /// <summary>Gets the type of the catalog.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public Type CatalogType { get; }
}
