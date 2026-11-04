using CyberInternauts.CmdStarter.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CyberInternauts.CmdStarter;

/// <summary>Registers CmdStarter services in an <see cref="IServiceCollection"/>.</summary>
public static class CmdStarterServiceCollectionExtensions
{
    /// <summary>Registers a configured command starter and its command types.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the starter.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddCmdStarter(this IServiceCollection services, Action<CmdStarterOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = new CmdStarterOptions();
        configure?.Invoke(options);
        Starter starter = options.ReplInputProvider is null ? new Starter() : new Repl.ReplStarter(options.ReplInputProvider);
        if (options.Assemblies.Count > 0) starter.Assemblies = starter.Assemblies.AddRange(options.Assemblies);
        foreach (var starterConfiguration in options.StarterConfigurations) starterConfiguration(starter);
        starter.FindCommandsTypes();
        foreach (var commandType in starter.CommandsTypes)
        {
            if (!Helper.DeclaresGetInstance(commandType)) services.TryAddTransient(commandType);
        }
        foreach (var commandType in starter.MethodCommandsTypes.Where(type => !type.IsAbstract))
        {
            services.TryAddTransient(commandType);
        }
        services.TryAddSingleton<Starter>(provider =>
        {
            starter.ServiceProvider ??= provider;
            return starter;
        });
        if (starter is Repl.ReplStarter)
        {
            services.TryAddSingleton<Repl.ReplStarter>(provider => (Repl.ReplStarter)provider.GetRequiredService<Starter>());
        }

        return services;
    }
}
