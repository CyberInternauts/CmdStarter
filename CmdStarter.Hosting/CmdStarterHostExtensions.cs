using CyberInternauts.CmdStarter;
using CyberInternauts.CmdStarter.Repl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CyberInternauts.CmdStarter.Hosting;

/// <summary>Extensions for running CmdStarter through a generic host.</summary>
public static class CmdStarterHostExtensions
{
    /// <summary>Starts hosted services, runs the CmdStarter command or REPL, and stops the host.</summary>
    /// <param name="host">The built host.</param>
    /// <param name="args">Command line arguments.</param>
    /// <param name="cancellationToken">A token used to cancel command execution.</param>
    /// <returns>The command exit code, or zero after the REPL exits.</returns>
    public static async Task<int> RunCmdStarterAsync(this IHost host, string[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(args);
        try
        {
            await host.StartAsync(cancellationToken);
            var starter = host.Services.GetRequiredService<Starter>();
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping);
            if (starter is ReplStarter replStarter)
            {
                if (args.Length == 0)
                {
                    await replStarter.Launch(linkedCancellation.Token);
                }
                else
                {
                    await replStarter.Launch(args, linkedCancellation.Token);
                }

                return 0;
            }

            return await starter.Start(args, linkedCancellation.Token);
        }
        finally
        {
            await host.StopAsync(CancellationToken.None);
        }
    }
}
