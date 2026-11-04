using System.CommandLine;

namespace CyberInternauts.CmdStarter.Catalog;

/// <summary>
/// Creates and reads the System.CommandLine symbols of one value type, without building a generic type at run time. The generator of the package
/// writes the registration of one factory for each value type that the commands of an assembly use (see <see cref="ICommandCatalogBuilder.AddSymbolType{T}"/>);
/// for any other type, CmdStarter makes the same factory by reflection.
/// </summary>
public interface ISymbolFactory
{
    /// <summary>Gets the type of the values of the symbols that this factory creates.</summary>
    Type ValueType { get; }

    /// <summary>
    /// Gets the value of a parameterless construction of <see cref="ValueType"/> for a value type, boxed (its parameterless constructor when it declares one),
    /// and <see langword="null"/> for a reference type. For a nullable value type, it is <see langword="null"/>.
    /// </summary>
    object? DefaultValue { get; }

    /// <summary>Creates an option of <see cref="ValueType"/>.</summary>
    /// <param name="name">The name of the option.</param>
    /// <returns>The option.</returns>
    Option CreateOption(string name);

    /// <summary>Creates an argument of <see cref="ValueType"/>.</summary>
    /// <param name="name">The name of the argument.</param>
    /// <returns>The argument.</returns>
    Argument CreateArgument(string name);

    /// <summary>Sets the value that an option gives when it is not on the command line.</summary>
    /// <param name="optionSymbol">An option made by this factory.</param>
    /// <param name="value">The value, of <see cref="ValueType"/>.</param>
    void SetDefault(Option optionSymbol, object? value);

    /// <summary>Sets the value that an argument gives when it is not on the command line.</summary>
    /// <param name="argumentSymbol">An argument made by this factory.</param>
    /// <param name="value">The value, of <see cref="ValueType"/>.</param>
    void SetDefault(Argument argumentSymbol, object? value);

    /// <summary>Reads the value of an option in a parse result.</summary>
    /// <param name="parseResult">The parse result.</param>
    /// <param name="optionSymbol">An option made by this factory.</param>
    /// <returns>The value, boxed.</returns>
    object? GetValue(ParseResult parseResult, Option optionSymbol);

    /// <summary>Reads the value of an argument in a parse result.</summary>
    /// <param name="parseResult">The parse result.</param>
    /// <param name="argumentSymbol">An argument made by this factory.</param>
    /// <returns>The value, boxed.</returns>
    object? GetValue(ParseResult parseResult, Argument argumentSymbol);
}
