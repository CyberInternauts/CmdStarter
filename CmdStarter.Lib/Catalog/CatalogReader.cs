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
    // False when catalogs are not used or when the assembly has no catalog: the caller scans it. A catalog that cannot be created, or whose Describe fails, is a configuration error.
    internal static bool TryRead(Assembly assembly, bool useCatalogs, [NotNullWhen(true)] out CatalogContent? content)
    {
        content = null;
        if (!useCatalogs)
        {
            return false;
        }

        var attribute = assembly.GetCustomAttribute<CommandCatalogAttribute>();
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
        internal HashSet<Type> CommandTypes { get; } = new();

        internal HashSet<Type> MethodCommandTypes { get; } = new();

        internal HashSet<Type> GlobalOptionsTypes { get; } = new();

        public void AddCommand<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TCommand>() where TCommand : class, IStarterCommand => CommandTypes.Add(typeof(TCommand));

        public void AddMethodCommands([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type declaringType)
        {
            ArgumentNullException.ThrowIfNull(declaringType);
            MethodCommandTypes.Add(declaringType);
        }

        public void AddGlobalOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TContainer>() where TContainer : class, IGlobalOptionsContainer => GlobalOptionsTypes.Add(typeof(TContainer));

        public void AddSymbolType<T>() => SymbolFactories.Register(new SymbolFactory<T>());
    }
}
