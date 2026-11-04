using Microsoft.Extensions.DependencyInjection;

namespace com.cyberinternauts.csharp.CmdStarter.Lib;

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

    /// <summary>Disposes the service provider.</summary>
    public void Dispose() => serviceProvider.Dispose();

    /// <summary>Asynchronously disposes the service provider.</summary>
    public ValueTask DisposeAsync() => serviceProvider.DisposeAsync();
}
