namespace CyberInternauts.CmdStarter.Catalog;

/// <summary>
/// The list of the command types of an assembly, written at compile time. A <see cref="Starter"/> creates the catalog that a
/// <see cref="CommandCatalogAttribute"/> designates, with its public parameterless constructor, and calls <see cref="Describe(ICommandCatalogBuilder)"/> once.
/// An assembly that has no catalog is scanned by reflection.
/// </summary>
public interface ICommandCatalog
{
    /// <summary>Declares the types of the assembly: the commands, the classes of method commands and the global options containers.</summary>
    /// <param name="builder">Receives the declarations.</param>
    void Describe(ICommandCatalogBuilder builder);
}
