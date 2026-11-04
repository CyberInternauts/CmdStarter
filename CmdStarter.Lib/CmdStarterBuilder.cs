using System.Reflection;
using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using com.cyberinternauts.csharp.CmdStarter.Lib.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace com.cyberinternauts.csharp.CmdStarter.Lib;

/// <summary>Configures and builds a CmdStarter application backed by Microsoft.Extensions.DependencyInjection.</summary>
public sealed class CmdStarterBuilder
{
    private readonly List<Assembly> assemblies = new();
    private readonly List<Action<Starter>> starterConfigurations = new();
    private bool built;

    /// <summary>Gets the service collection used to register application services.</summary>
    public IServiceCollection Services { get; } = new ServiceCollection();

    /// <summary>Gets the options used when building the service provider.</summary>
    public ServiceProviderOptions ServiceProviderOptions { get; } = new() { ValidateScopes = true, ValidateOnBuild = true };

    /// <summary>Adds an assembly to search for commands.</summary>
    /// <param name="assembly">The assembly to search.</param>
    /// <returns>This builder.</returns>
    public CmdStarterBuilder AddCommandsFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        if (!assemblies.Contains(assembly)) assemblies.Add(assembly);
        return this;
    }

    /// <summary>Adds a configuration action for the Starter.</summary>
    /// <param name="configure">The configuration action.</param>
    /// <returns>This builder.</returns>
    public CmdStarterBuilder ConfigureStarter(Action<Starter> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        starterConfigurations.Add(configure);
        return this;
    }

    /// <summary>Builds the application and its service provider.</summary>
    /// <exception cref="InvalidOperationException">The application has already been built.</exception>
    public CmdStarterApp Build()
    {
        if (built) throw new InvalidOperationException("The application has already been built.");
        built = true;
        var starter = new Starter();
        if (assemblies.Count > 0) starter.Assemblies = starter.Assemblies.AddRange(assemblies);
        foreach (var configure in starterConfigurations) configure(starter);
        starter.FindCommandsTypes();
        foreach (var commandType in starter.CommandsTypes)
        {
            if (!Helper.DeclaresGetInstance(commandType)) Services.TryAddTransient(commandType);
        }
        foreach (var commandType in starter.MethodCommandsTypes.Where(type => !type.IsAbstract))
        {
            Services.TryAddTransient(commandType);
        }
        var provider = Services.BuildServiceProvider(ServiceProviderOptions);
        starter.ServiceProvider = provider;
        return new CmdStarterApp(starter, provider);
    }
}
