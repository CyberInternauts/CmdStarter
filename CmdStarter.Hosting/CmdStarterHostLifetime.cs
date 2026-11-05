using Microsoft.Extensions.Hosting;

namespace CyberInternauts.CmdStarter.Hosting;

/// <summary>
/// A host lifetime that leaves console cancellation and startup output to the CLI command runner.
/// The default ConsoleLifetime intercepts Ctrl+C to stop the host and writes “Application started…”
/// to standard output. System.CommandLine handles Ctrl+C for a command, and ReplStarter handles it for the REPL.
/// </summary>
internal sealed class CmdStarterHostLifetime : IHostLifetime
{
    public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
