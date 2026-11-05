using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CyberInternauts.CmdStarter.Interfaces;
using CyberInternauts.CmdStarter.Loader;

namespace CyberInternauts.CmdStarter;

/// <summary>Configures a CmdStarter registration in a service collection.</summary>
public sealed class CmdStarterOptions
{
    internal List<Assembly> Assemblies { get; } = [];
    internal List<Action<Starter>> StarterConfigurations { get; } = [];
    internal IReplInputProvider? ReplInputProvider { get; private set; }

    /// <summary>Adds an assembly to search for commands.</summary>
    /// <param name="assembly">The assembly to search.</param>
    /// <returns>This options instance.</returns>
    public CmdStarterOptions AddCommandsFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        if (!Assemblies.Contains(assembly)) Assemblies.Add(assembly);
        return this;
    }

    /// <summary>Adds a configuration action for the starter.</summary>
    /// <param name="configure">The configuration action.</param>
    /// <returns>This options instance.</returns>
    public CmdStarterOptions ConfigureStarter(Action<Starter> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        StarterConfigurations.Add(configure);
        return this;
    }

    /// <summary>Adds a command filter type to the configured starter.</summary>
    /// <typeparam name="TFilter">The filter type.</typeparam>
    /// <returns>This options instance.</returns>
    public CmdStarterOptions AddFilter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFilter>() where TFilter : class, ICommandFilter
    {
        return ConfigureStarter(starter => starter.AddFilter<TFilter>());
    }

    /// <summary>Configures the application to run in REPL mode.</summary>
    /// <param name="inputProvider">Provides lines to the REPL.</param>
    /// <returns>This options instance.</returns>
    public CmdStarterOptions UseRepl(IReplInputProvider inputProvider)
    {
        ArgumentNullException.ThrowIfNull(inputProvider);
        ReplInputProvider = inputProvider;
        return this;
    }

    /// <summary>Configures the starter with an explicit command tree.</summary>
    /// <param name="define">Callback that defines the command tree.</param>
    /// <returns>This options instance.</returns>
    public CmdStarterOptions UseTree(Action<CommandTreeDefinition> define)
    {
        ArgumentNullException.ThrowIfNull(define);
        return ConfigureStarter(starter => starter.UseTree(define));
    }
}
