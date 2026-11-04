using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter;

/// <summary>A built CmdStarter application and its Microsoft DI service provider.</summary>
public sealed class CmdStarterApp : IDisposable, IAsyncDisposable
{
    private readonly ServiceProvider serviceProvider;

    internal CmdStarterApp(Starter starter, ServiceProvider serviceProvider)
    {
        Starter = starter;
        this.serviceProvider = serviceProvider;
    }

    /// <summary>Gets the configured command starter.</summary>
    public Starter Starter { get; }

    /// <summary>Gets the application's service provider.</summary>
    public IServiceProvider Services => serviceProvider;

    /// <summary>Runs the application with command line arguments.</summary>
    public Task<int> RunAsync(string[] args) => Starter.Start(args);

    /// <summary>Runs the application with a command line string.</summary>
    public Task<int> RunAsync(string args) => Starter.Start(args);

    /// <summary>Runs the application REPL loop.</summary>
    /// <param name="cancellationToken">Token used to stop the loop and cancel commands.</param>
    /// <returns>A task representing the REPL loop.</returns>
    /// <exception cref="InvalidOperationException">The builder was not configured with <see cref="CmdStarterBuilder.UseRepl(CyberInternauts.CmdStarter.Interfaces.IReplInputProvider)"/>.</exception>
    public Task RunReplAsync(CancellationToken cancellationToken = default)
    {
        if (Starter is not Repl.ReplStarter replStarter)
        {
            throw new InvalidOperationException("Call CmdStarterBuilder.UseRepl before running a REPL.");
        }

        return replStarter.Launch(cancellationToken);
    }

    /// <summary>Runs an initial command and then the application REPL loop.</summary>
    /// <param name="args">Arguments for the initial command.</param>
    /// <param name="cancellationToken">Token used to stop the loop and cancel commands.</param>
    /// <returns>A task representing the REPL loop.</returns>
    /// <exception cref="InvalidOperationException">The builder was not configured with <see cref="CmdStarterBuilder.UseRepl(CyberInternauts.CmdStarter.Interfaces.IReplInputProvider)"/>.</exception>
    public Task RunReplAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (Starter is not Repl.ReplStarter replStarter)
        {
            throw new InvalidOperationException("Call CmdStarterBuilder.UseRepl before running a REPL.");
        }

        return replStarter.Launch(args, cancellationToken);
    }

    /// <summary>Disposes the service provider.</summary>
    public void Dispose() => serviceProvider.Dispose();

    /// <summary>Asynchronously disposes the service provider.</summary>
    public ValueTask DisposeAsync() => serviceProvider.DisposeAsync();
}
