using System.Diagnostics.CodeAnalysis;
using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Catalog;

/// <summary>
/// Receives the declarations of an <see cref="ICommandCatalog"/>. A declaration only tells the search where to look: it validates nothing, and the
/// filters of the search (<see cref="Starter.Namespaces"/>, <see cref="Starter.Classes"/>) apply to the declared types as they apply to scanned ones.
/// A catalog shares the filter of the scan: a type that the scan would not find is ignored, whatever the catalog declares. The scan finds a command class
/// that is not abstract and has a namespace, a class that has a namespace and declares a <c>[Command]</c> method or is marked <c>[Command]</c>,
/// and a class that implements <see cref="IGlobalOptionsContainer"/>.
/// </summary>
public interface ICommandCatalogBuilder
{
    /// <summary>Declares a command class.</summary>
    /// <typeparam name="TCommand">The command class.</typeparam>
    void AddCommand<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TCommand>() where TCommand : class;

    /// <summary>Declares a class whose methods marked <c>[Command]</c> are commands. A type is passed, not a type argument, because the class is often static.</summary>
    /// <param name="declaringType">The class that declares the method commands.</param>
    void AddMethodCommands([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type declaringType);

    /// <summary>Declares a global options container.</summary>
    /// <typeparam name="TContainer">The container class.</typeparam>
    void AddGlobalOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TContainer>() where TContainer : class, IGlobalOptionsContainer;

    /// <summary>
    /// Declares a type of value that options and arguments use, so that CmdStarter has a typed factory of symbols for it (<see cref="ISymbolFactory"/>)
    /// that exists in the assembly of the catalog. The first declaration of a type is kept for the whole process; a later one is ignored.
    /// </summary>
    /// <typeparam name="T">The type of the values. Its parameterless constructor is kept when the application is trimmed, because the default value of a value type is the one it gives.</typeparam>
    void AddSymbolType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>();
}
