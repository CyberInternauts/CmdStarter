using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace CyberInternauts.CmdStarter.Catalog;

// The factories of the process, by value type. They are static, like the caches of getters that they replace.
internal static class SymbolFactories
{
    // The factories that a catalog registered, and the fallbacks made by reflection for the other types. Two dictionaries, so that a fallback never blocks a registration.
    private static readonly ConcurrentDictionary<Type, ISymbolFactory> registered = new();
    private static readonly ConcurrentDictionary<Type, ISymbolFactory> fallbacks = new();

    // The registered factory, else the fallback, made once.
    internal static ISymbolFactory For(Type valueType) =>
        registered.TryGetValue(valueType, out var factory) ? factory : fallbacks.GetOrAdd(valueType, CreateFallback);

    // The first registration of a type wins and replaces a fallback that was already made; a later one is ignored.
    internal static void Register(ISymbolFactory factory)
    {
        if (registered.TryAdd(factory.ValueType, factory))
        {
            fallbacks.TryRemove(factory.ValueType, out _);
        }
    }

    // True when the factory of the type comes from a registration, false when it is a fallback.
    internal static bool IsGenerated(Type valueType) => registered.ContainsKey(valueType);

    // The only place that builds a generic type at run time for the symbols: it behaves as the factory that the generator would have registered.
    [UnconditionalSuppressMessage("Trimming", "IL2071", Justification = "The fallback only runs for a value type that no command catalog registered, which the generator does not leave out for a type it can write. A type it cannot write is not supported under trimming; on CoreCLR the type is kept, because the application uses it.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The fallback only runs for a value type that no command catalog registered: the generator registers the value types that the commands use, so that their factories exist in the application. Under Native AOT, a value type it could not register is not supported.")]
    internal static ISymbolFactory CreateFallback(Type valueType) =>
        (ISymbolFactory)Activator.CreateInstance(typeof(SymbolFactory<>).MakeGenericType(valueType))!;
}
