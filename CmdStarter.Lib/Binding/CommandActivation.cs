using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Binding;

internal sealed class CommandActivation : IDisposable, IAsyncDisposable
{
    private readonly bool ownsInstance;
    private readonly AsyncServiceScope? scope;
    private bool disposed;

    internal CommandActivation(IStarterCommand instance, bool ownsInstance, AsyncServiceScope? scope = null)
    {
        Instance = instance;
        this.ownsInstance = ownsInstance;
        this.scope = scope;
    }

    internal IStarterCommand Instance { get; }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        if (ownsInstance && Instance is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (ownsInstance && Instance is IDisposable disposable)
        {
            disposable.Dispose();
        }
        if (scope.HasValue) await scope.Value.DisposeAsync();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (ownsInstance && Instance is IDisposable disposable)
        {
            disposable.Dispose();
        }
        else if (ownsInstance && Instance is IAsyncDisposable asyncDisposable)
        {
            // InstantiateCommands is synchronous and public, so async-only instances must be synchronously drained here.
            asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        if (scope.HasValue) scope.Value.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
