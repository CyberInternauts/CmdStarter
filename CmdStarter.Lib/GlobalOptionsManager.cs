using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using System.CommandLine;
using System.CommandLine.Parsing;
using static com.cyberinternauts.csharp.CmdStarter.Lib.Reflection.Helper;
using com.cyberinternauts.csharp.CmdStarter.Lib.Binding;

namespace com.cyberinternauts.csharp.CmdStarter.Lib;

/// <summary>
/// Manager for all global options classes found
/// </summary>
public sealed class GlobalOptionsManager
{

    private static IEnumerable<Type>? foundTypes;
    private readonly Dictionary<Type, object> globalOptions = new();
    private readonly Dictionary<Type, List<OptionBinding>> optionBindings = new();
    private readonly Starter starter;

    internal GlobalOptionsManager(Starter starter)
    {
        this.starter = starter;
        FindTypes();
    }

    /// <summary>
    /// List of all global options types
    /// </summary>
    public List<Type> GlobalOptionsTypes { get; private set; } = new();

    /// <summary>
    /// Retrieve a global options container of a specific type
    /// </summary>
    /// <typeparam name="GlobalOptionsType">Global options container type to look for</typeparam>
    /// <returns>An <see cref="IGlobalOptionsContainer"/> or null if nothing found</returns>
    public GlobalOptionsType? GetGlobalOptions<GlobalOptionsType>() where GlobalOptionsType : class, IGlobalOptionsContainer
    {
        var type = typeof(GlobalOptionsType);

        return globalOptions.TryGetValue(type, out object? value) ? (GlobalOptionsType)value : null;
    }

    internal object? GetGlobalOptions(Type type) => globalOptions.TryGetValue(type, out var value) ? value : null;

    /// <summary>
    /// Add/change a global options container object based on its type
    /// </summary>
    /// <typeparam name="GlobalOptionsType">Global options container type to use as key</typeparam>
    /// <param name="globalOption"></param>
    internal void SetGlobalOptions<GlobalOptionsType>(GlobalOptionsType globalOption) where GlobalOptionsType : class, IGlobalOptionsContainer
    {
        var type = typeof(GlobalOptionsType);

        if (globalOptions.ContainsKey(type))
        {
            globalOptions[type] = globalOption;
        }
        else
        {
            globalOptions.Add(type, globalOption);
        }
    }

    internal void FilterTypes()
    {
        GlobalOptionsTypes.Clear();

        var filteredTypes = foundTypes!; // foundTypes can't be null as initialized in constructor
        if (!starter.Assemblies.IsEmpty)
        {
            filteredTypes = filteredTypes.Where(type => starter.Assemblies.Contains(type.Assembly));
        }
        filteredTypes = FilterTypesByNamespaces(filteredTypes, starter.Namespaces.ToList());
        filteredTypes = FilterTypesByClasses(filteredTypes, starter.Classes.ToList());

        GlobalOptionsTypes.AddRange(filteredTypes.OrderBy(type => type.FullName, StringComparer.Ordinal));
    }

    internal void LoadOptions(Command receptacle)
    {
        optionBindings.Clear();
        foreach (var type in GlobalOptionsTypes)
        {
            optionBindings[type] = Reflection.Loader.LoadOptions(type, receptacle);
        }
    }

    internal void BindGlobalOptions(ParseResult parseResult)
    {
        foreach (var type in GlobalOptionsTypes)
        {
            if (optionBindings.TryGetValue(type, out var bindings))
            {
                globalOptions[type] = CommandBinder.CreateGlobalOptions(parseResult, type, bindings);
            }
        }
    }

    private static void FindTypes()
    {
        if (foundTypes != null) return;

        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && t.IsAssignableTo(typeof(IGlobalOptionsContainer)));

        foundTypes = new List<Type>(types);
    }
}
