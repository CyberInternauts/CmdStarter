using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace CyberInternauts.CmdStarter.Catalog;

// The factory of one value type. The code that the generator writes instantiates it with a type argument known at compile time, which is what makes
// Option<T>, Argument<T> and ParseResult.GetValue<T> exist statically in the application; SymbolFactories builds it by reflection for any other type.
internal sealed class SymbolFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> : ISymbolFactory
{
    public Type ValueType => typeof(T);

    // A value type is constructed as Activator.CreateInstance(type) did: its parameterless constructor runs when it declares one. A reference type gives null.
    public object? DefaultValue => typeof(T).IsValueType ? Activator.CreateInstance<T>() : null;

    public Option CreateOption(string name) => new Option<T>(name, Array.Empty<string>());

    public Argument CreateArgument(string name) => new Argument<T>(name);

    public void SetDefault(Option optionSymbol, object? value) => ((Option<T>)optionSymbol).DefaultValueFactory = _ => (T)value!;

    public void SetDefault(Argument argumentSymbol, object? value) => ((Argument<T>)argumentSymbol).DefaultValueFactory = _ => (T)value!;

    public object? GetValue(ParseResult parseResult, Option optionSymbol) => parseResult.GetValue((Option<T>)optionSymbol);

    public object? GetValue(ParseResult parseResult, Argument argumentSymbol) => parseResult.GetValue((Argument<T>)argumentSymbol);
}
