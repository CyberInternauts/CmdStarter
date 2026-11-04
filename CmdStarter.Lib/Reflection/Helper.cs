using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CyberInternauts.CmdStarter.Reflection;

/// <summary>
/// Reflection helper class specific to <see cref="IStarterCommand"/>
/// </summary>
public static class Helper
{

    private static List<string>? interfacePropertiesNames;

    // Never invalidated: the search only reads the type hierarchy.
    private static readonly ConcurrentDictionary<Type, MethodInfo> getInstanceMethods = new();

    /// <summary>
    /// Find the most derived <see cref="IStarterCommand.GetInstance{TCommand}"/> or use default implementation
    /// </summary>
    /// <param name="commandType">Command type to look for <see cref="IStarterCommand.GetInstance{TCommand}"/></param>
    /// <returns>A method info object of the method to execute. The result only depends on the type, so it is computed once per type.</returns>
    public static MethodInfo FindGetInstanceMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type commandType)
    {
        // Not GetOrAdd with a method group: a delegate to a method whose parameter is annotated is a reflection access for the trimmer. Two threads that search at once find the same method.
        if (!getInstanceMethods.TryGetValue(commandType, out var method))
        {
            method = SearchGetInstanceMethod(commandType);
            getInstanceMethods[commandType] = method;
        }

        return method;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "GetMethod reads the public methods of the base types of a command, and a command type that is kept (the command catalog keeps all its members) keeps the public methods that it inherits.")]
    [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "The generic method is IStarterCommand.GetInstance, instantiated over a command class, which is a reference type: the instantiation shares the code of the canonical form and needs nothing of its own.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The generic method is IStarterCommand.GetInstance, instantiated over a command class, which is a reference type: the instantiation shares the code of the canonical form, so no native code has to be generated at run time.")]
    private static MethodInfo SearchGetInstanceMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type commandType)
    {
        const string methodName = nameof(IStarterCommand.GetInstance);

        // Look through classes hierarchy from top to bottom
        MethodInfo? methodInfo = null;
        var currentType = commandType;
        while (
            methodInfo == null
            && currentType != null
            && currentType.IsAssignableTo(typeof(IStarterCommand))
            && !currentType.Equals(typeof(StarterCommand)) // Ensures not looping back
            )
        {
            methodInfo = currentType.GetMethod(methodName);
            currentType = currentType.BaseType;
        }
        // Use default interface method implementation
        if (methodInfo == null)
        {
            methodInfo = typeof(IStarterCommand).GetMethod(methodName)!;
        }
        var method = methodInfo.MakeGenericMethod(commandType);
        return method;
    }

    internal static bool DeclaresGetInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type commandType) =>
        FindGetInstanceMethod(commandType).DeclaringType != typeof(IStarterCommand);

    /// <inheritdoc cref="GetProperties(Type)"/>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "The type is the one of an instance that exists, which is a command or a global options container: the command catalog keeps all the members of the types it declares.")]
    public static IEnumerable<PropertyInfo> GetProperties(object obj)
    {
        return GetProperties(obj.GetType());
    }

    /// <summary>
    /// Retrieve properties on a type that are considered as options
    /// </summary>
    /// <param name="type">Type to look properties on</param>
    /// <returns>A list of properties</returns>
    public static IEnumerable<PropertyInfo> GetProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        LoadInterfaceProperties();

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p =>
                p.CanWrite && p.CanRead
                &&
                (
                  p.DeclaringType!.IsAssignableTo(typeof(IStarterCommand))
                  && !interfacePropertiesNames!.Contains(p.Name) // Can't be null because already loaded
                ||
                p.DeclaringType!.IsAssignableTo(typeof(IGlobalOptionsContainer)))
                &&
                !Attribute.IsDefined(p.DeclaringType, typeof(AllOptionsExcludedAttribute))
            );

        bool hasAnyOptionAttribute = properties.Any(property =>
            property.GetCustomAttribute<OptionAttribute>() is not null);

        if (!hasAnyOptionAttribute)
        {
            properties = properties.Where(property =>
                !Attribute.IsDefined(property, typeof(NotOptionAttribute)));
        }
        else
        {
            properties = properties.Where(property =>
            {
                return Attribute.IsDefined(property, typeof(OptionAttribute))
                    && !Attribute.IsDefined(property, typeof(NotOptionAttribute));
            });
        }

        return properties;
    }

    /// <summary>
    /// Filter a list of types using namespaces
    /// </summary>
    /// <param name="commandsTypes">Commands types to filter</param>
    /// <param name="namespaces">Namespaces to accept or reject</param>
    /// <returns>The filtered list of commands types</returns>
    public static IEnumerable<Type> FilterTypesByNamespaces(IEnumerable<Type> commandsTypes, List<string> namespaces)
    {
        var nbCommands = commandsTypes.Count();
        if (nbCommands == 0 || namespaces.Count == 0) return commandsTypes;

        var namespacesIncluded = namespaces.Where(n => !string.IsNullOrWhiteSpace(n) && !n.StartsWith(Starter.ExclusionSymbol, StringComparison.Ordinal));
        var hasIncluded = namespacesIncluded.Any();
        var namespacesExcluded = namespaces.Where(n => !string.IsNullOrWhiteSpace(n) && n.StartsWith(Starter.ExclusionSymbol, StringComparison.Ordinal));

        commandsTypes = commandsTypes.Where(c =>
        {
            var outNamespaces = namespacesExcluded.Any(n => c.Namespace?.StartsWith(n[1..], StringComparison.Ordinal) ?? false);
            var inNamespaces = !hasIncluded || namespacesIncluded.Any(n => c.Namespace?.StartsWith(n, StringComparison.Ordinal) ?? false);

            return !outNamespaces && inNamespaces;
        });

        return commandsTypes;
    }

    /// <summary>
    /// Filter a list of types using full classes names
    /// </summary>
    /// <param name="commandsTypes">Commands types to filter</param>
    /// <param name="classes">Classes names to filter: Wildcard can be used (?, ??, *, **)</param>
    /// <returns>The filtered list of commands types</returns>
    public static IEnumerable<Type> FilterTypesByClasses(IEnumerable<Type> commandsTypes, List<string> classes)
    {
        var nbCommands = commandsTypes.Count();
        if (nbCommands == 0 || classes.Count == 0) return commandsTypes;

        bool onlyExclude = classes.All(filter => filter.StartsWith(Starter.ExclusionSymbol, StringComparison.Ordinal));

        Regex[] excludes = [.. classes.Where(filter => filter.StartsWith(Starter.ExclusionSymbol, StringComparison.Ordinal))
            .Select(filter =>
            {
                var pattern = WildcardsToRegex(filter[1..]);
                return new Regex(pattern, RegexOptions.RightToLeft);
            })];

        Regex[] filters = [.. classes.Where(filter => !filter.StartsWith(Starter.ExclusionSymbol, StringComparison.Ordinal))
            .Select(filter =>
            {
                var pattern = WildcardsToRegex(filter);
                return new Regex(pattern, RegexOptions.RightToLeft);
            })];

        commandsTypes = commandsTypes.Where(type =>
        {
            bool included = onlyExclude || filters.Any(rgx => rgx.IsMatch(type.FullName ?? string.Empty));

            bool excluded = excludes.Any(rgx => rgx.IsMatch(type.FullName ?? string.Empty));

            return included && !excluded;
        });

        return commandsTypes;
    }

    // The filter is text: every character is escaped first, then the escaped wildcards become their regular expression.
    private static string WildcardsToRegex(string wildcard)
    {
        const string STAR_PLACEHOLDER = "<-starplaceholder->";

        return @$"(\.|^){Regex.Escape(wildcard)}$"
            .Replace(Regex.Escape(Starter.AnyCharSymbolIncludeDots), ".")
            .Replace(Regex.Escape(Starter.AnyCharSymbol), @"\w")
            .Replace(Regex.Escape(Starter.MultiAnyCharSymbolIncludeDots), @$".{STAR_PLACEHOLDER}")
            .Replace(Regex.Escape(Starter.MultiAnyCharSymbol), @"\w*")
            .Replace(STAR_PLACEHOLDER, "*");
    }

    /// <summary>
    /// Gets the types of an assembly that can be loaded: the others (a type that depends on a missing assembly) are left out instead of failing the whole assembly.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The scan of the types of an assembly only runs for an assembly that has no command catalog, which is not supported in a trimmed application: an assembly that has one is read from its catalog, which keeps the types it declares.")]
    internal static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

    private static void LoadInterfaceProperties()
    {
        if (interfacePropertiesNames != null) return;

        interfacePropertiesNames = [.. typeof(IStarterCommand).GetProperties().Select(p => p.Name)];
    }
}
