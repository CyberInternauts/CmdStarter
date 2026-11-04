using CyberInternauts.CmdStarter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CyberInternauts.CmdStarter.Hosting;

/// <summary>Extensions for registering CmdStarter with a generic host builder.</summary>
public static class CmdStarterHostApplicationBuilderExtensions
{
    /// <summary>Registers CmdStarter and configures it with the host services.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">Configures the starter.</param>
    /// <returns>The host application builder.</returns>
    public static IHostApplicationBuilder AddCmdStarter(this IHostApplicationBuilder builder, Action<CmdStarterOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddCmdStarter(configure);
        builder.Services.Replace(ServiceDescriptor.Singleton<IHostLifetime, CmdStarterHostLifetime>());
        return builder;
    }
}
