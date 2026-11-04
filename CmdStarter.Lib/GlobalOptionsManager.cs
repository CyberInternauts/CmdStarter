using CyberInternauts.CmdStarter.Catalog;
using CyberInternauts.CmdStarter.Exceptions;
using CyberInternauts.CmdStarter.Interfaces;
using System.CommandLine;
using System.CommandLine.Parsing;
using static CyberInternauts.CmdStarter.Reflection.Helper;
using CyberInternauts.CmdStarter.Binding;
using System.Reflection;

namespace CyberInternauts.CmdStarter;

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
    }

    /// <summary>
    /// List of all global options types
    /// </summary>
    public List<Type> GlobalOptionsTypes { get; private set; } = new();

    /// <summary>
    /// Retrieve a global options container of a specific type
    /// </summary>
    /// <typeparam name="TGlobalOptions">Global options container type to look for</typeparam>
    /// <returns>An <see cref="IGlobalOptionsContainer"/> or null if nothing found</returns>
    public TGlobalOptions? GetGlobalOptions<TGlobalOptions>() where TGlobalOptions : class, IGlobalOptionsContainer
    {
        var type = typeof(TGlobalOptions);

        return globalOptions.TryGetValue(type, out object? value) ? (TGlobalOptions)value : null;
    }

    // Only called for a container that the filters admit: it is rebuilt by BindGlobalOptions at each parse.
    internal object GetGlobalOptions(Type type) => globalOptions[type];

    internal void FilterTypes()
    {
        GlobalOptionsTypes.Clear();

        // Found here and not in the constructor: a catalog that fails is a configuration error of the tree, never of "new Starter()", and the setting UseCommandCatalogs is the one the starter has by now.
        FindTypes(starter.UseCommandCatalogs, starter.DynamicCodeSupported);
        var filteredTypes = foundTypes!;
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
            var bindings = Reflection.Loader.LoadOptions(type, receptacle);
            optionBindings[type] = bindings;
            var sample = CreateSample(type);
            foreach (var binding in bindings)
            {
                Reflection.Loader.SetImplicitDefault(binding.Option, binding.Property.GetValue(sample));
            }
        }
    }

    private static object CreateSample(Type type)
    {
        try
        {
            return Activator.CreateInstance(type)!;
        }
        catch (MissingMethodException exception)
        {
            throw new InvalidAttributeException($"The global options container '{type.FullName}' needs a public parameterless constructor. {exception.Message}");
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

    private static void FindTypes(bool useCatalogs, bool dynamicCodeSupported)
    {
        if (foundTypes != null) return;

        foundTypes = FindContainerTypes(AppDomain.CurrentDomain.GetAssemblies(), useCatalogs, dynamicCodeSupported);
    }

    // Assembly by assembly, like the search of the commands: the catalog of an assembly, else its types. The same predicate applies to both.
    // Without dynamic code (Native AOT), the references of an assembly cannot be asked and only the catalogs are read: an assembly that has none is not scanned.
    internal static List<Type> FindContainerTypes(IEnumerable<Assembly> assemblies, bool useCatalogs, bool dynamicCodeSupported)
    {
        IEnumerable<Type> Read(Assembly assembly)
        {
            var canHaveCatalog = !useCatalogs || !dynamicCodeSupported || CatalogReader.ReferencesLibrary(assembly);
            if (CatalogReader.TryRead(assembly, useCatalogs, canHaveCatalog, out var catalog))
            {
                return catalog.GlobalOptionsTypes;
            }

            return dynamicCodeSupported ? GetLoadableTypes(assembly) : Enumerable.Empty<Type>();
        }

        return new List<Type>(assemblies.SelectMany(Read).Where(t => t.IsClass && t.IsAssignableTo(typeof(IGlobalOptionsContainer))));
    }
}
