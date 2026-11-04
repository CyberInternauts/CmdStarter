using System.Reflection;
using CyberInternauts.CmdStarter.Interfaces;
using CyberInternauts.CmdStarter.Loader;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter;

/// <summary>Configures and builds a CmdStarter application backed by Microsoft.Extensions.DependencyInjection.</summary>
public sealed class CmdStarterBuilder
{
    private readonly CmdStarterOptions options = new();
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
        options.AddCommandsFromAssembly(assembly);
        return this;
    }

    /// <summary>Adds a configuration action for the Starter.</summary>
    /// <param name="configure">The configuration action.</param>
    /// <returns>This builder.</returns>
    public CmdStarterBuilder ConfigureStarter(Action<Starter> configure)
    {
        options.ConfigureStarter(configure);
        return this;
    }

    /// <summary>Adds a command filter type to the configured starter.</summary>
    /// <typeparam name="TFilter">The filter type.</typeparam>
    /// <returns>This builder.</returns>
    public CmdStarterBuilder AddFilter<TFilter>() where TFilter : class, ICommandFilter
    {
        options.AddFilter<TFilter>();
        return this;
    }

    /// <summary>Configures the application to run in REPL mode.</summary>
    /// <param name="inputProvider">Provides lines to the REPL.</param>
    /// <returns>This builder.</returns>
    public CmdStarterBuilder UseRepl(IReplInputProvider inputProvider)
    {
        options.UseRepl(inputProvider);
        return this;
    }

    /// <summary>Configures the starter with an explicit command tree.</summary>
    /// <param name="define">Callback that defines the command tree.</param>
    /// <returns>This builder.</returns>
    public CmdStarterBuilder UseTree(Action<CommandTreeDefinition> define)
    {
        ArgumentNullException.ThrowIfNull(define);
        options.ConfigureStarter(starter => starter.UseTree(define));
        return this;
    }

    /// <summary>Builds the application and its service provider.</summary>
    /// <exception cref="InvalidOperationException">The application has already been built.</exception>
    public CmdStarterApp Build()
    {
        if (built) throw new InvalidOperationException("The application has already been built.");
        built = true;
        Services.AddCmdStarter(configure =>
        {
            foreach (var assembly in options.Assemblies) configure.AddCommandsFromAssembly(assembly);
            foreach (var starterConfiguration in options.StarterConfigurations) configure.ConfigureStarter(starterConfiguration);
            if (options.ReplInputProvider is not null) configure.UseRepl(options.ReplInputProvider);
        });
        var provider = Services.BuildServiceProvider(ServiceProviderOptions);
        var starter = provider.GetRequiredService<Starter>();
        return new CmdStarterApp(starter, provider);
    }
}
