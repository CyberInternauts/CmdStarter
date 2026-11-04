using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CyberInternauts.CmdStarter.Exceptions;
using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Catalog;

// What the search reads from the catalog of an assembly. The types are those that the catalog declares: the engine applies to them the predicate of the scan.
internal sealed class CatalogContent
{
    internal CatalogContent(IReadOnlyCollection<Type> commandTypes, IReadOnlyCollection<Type> methodCommandTypes, IReadOnlyCollection<Type> globalOptionsTypes)
    {
        CommandTypes = commandTypes;
        MethodCommandTypes = methodCommandTypes;
        GlobalOptionsTypes = globalOptionsTypes;
    }

    internal IReadOnlyCollection<Type> CommandTypes { get; }

    internal IReadOnlyCollection<Type> MethodCommandTypes { get; }

    internal IReadOnlyCollection<Type> GlobalOptionsTypes { get; }
}

// The one decision, shared by the search of the commands and the search of the global options containers: is this assembly read from its catalog or scanned?
internal static class CatalogReader
{
    private static readonly Assembly Library = typeof(CatalogReader).Assembly;
    private static readonly string? LibraryName = Library.GetName().Name;

    // Whether the assembly is the library or references it: only such an assembly can carry a catalog or declare method commands.
    // GetReferencedAssemblies is not supported by Native AOT: a caller that runs without dynamic code does not ask.
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The assemblies searched are those of the application, and an assembly that uses the library keeps its reference to it. The references are asked only when dynamic code is supported, never under Native AOT.")]
    internal static bool ReferencesLibrary(Assembly assembly) =>
        assembly == Library || assembly.GetReferencedAssemblies().Any(reference => reference.Name == LibraryName);

    // False when catalogs are not used, when the assembly cannot have a catalog, or when it has none: the caller scans it, or leaves it when nothing can be scanned.
    // A catalog that cannot be created, or whose Describe fails, is a configuration error.
    // canHaveCatalog is false for an assembly that does not reference the library: the attribute is not even read, because the attributes of an unrelated assembly may not be resolvable
    // (the satellite assembly of System.CommandLine in a trimmed application references a System.Runtime that the application no longer has).
    internal static bool TryRead(Assembly assembly, bool useCatalogs, bool canHaveCatalog, [NotNullWhen(true)] out CatalogContent? content)
    {
        content = null;
        if (!useCatalogs || !canHaveCatalog)
        {
            return false;
        }

        CommandCatalogAttribute? attribute;
        try
        {
            attribute = assembly.GetCustomAttribute<CommandCatalogAttribute>();
        }
        catch (Exception exception) when (exception is FileNotFoundException or FileLoadException or TypeLoadException)
        {
            // The attributes of the assembly cannot be loaded: it has no catalog that can be read, like a type that does not load is skipped by the scan.
            return false;
        }

        if (attribute is null)
        {
            return false;
        }

        var collector = new CatalogCollector();
        try
        {
            var catalog = (ICommandCatalog)Activator.CreateInstance(attribute.CatalogType)!;
            catalog.Describe(collector);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Activator wraps the exception of a constructor; the reason is the inner one. Like CreateSample, this adds the original message to the text.
            var reason = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
            throw new InvalidAttributeException($"The command catalog '{attribute.CatalogType.FullName}' of the assembly '{assembly.FullName}' could not be read: {reason.Message}");
        }

        content = new CatalogContent(collector.CommandTypes, collector.MethodCommandTypes, collector.GlobalOptionsTypes);
        return true;
    }

    private sealed class CatalogCollector : ICommandCatalogBuilder
    {
        // Sets: a type that a hand-written catalog declares twice is found once, as the scan finds it once.
        internal HashSet<Type> CommandTypes { get; } = [];

        internal HashSet<Type> MethodCommandTypes { get; } = [];

        internal HashSet<Type> GlobalOptionsTypes { get; } = [];

        public void AddCommand<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TCommand>() where TCommand : class, IStarterCommand => CommandTypes.Add(typeof(TCommand));

        public void AddMethodCommands([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type declaringType)
        {
            ArgumentNullException.ThrowIfNull(declaringType);
            MethodCommandTypes.Add(declaringType);
        }

        public void AddGlobalOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TContainer>() where TContainer : class, IGlobalOptionsContainer => GlobalOptionsTypes.Add(typeof(TContainer));

        public void AddSymbolType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>() => SymbolFactories.Register(new SymbolFactory<T>());
    }
}
