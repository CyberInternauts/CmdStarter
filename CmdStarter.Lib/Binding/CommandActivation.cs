using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Binding;

internal sealed class CommandActivation : IDisposable, IAsyncDisposable
{
    private readonly bool ownsInstance;
    private readonly AsyncServiceScope? scope;
    private bool disposed;

    internal CommandActivation(object? instance, bool ownsInstance, AsyncServiceScope? scope = null)
    {
        Instance = instance;
        this.ownsInstance = ownsInstance;
        this.scope = scope;
    }

    internal object? Instance { get; }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            if (ownsInstance && Instance is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (ownsInstance && Instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        finally
        {
            // The scope is released even when the command's own disposal throws.
            if (scope.HasValue) await scope.Value.DisposeAsync();
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            if (ownsInstance && Instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
            else if (ownsInstance && Instance is IAsyncDisposable asyncDisposable)
            {
                // InstantiateCommands is synchronous and public, so async-only instances must be synchronously drained here.
                asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }
        finally
        {
            // Never call the scope's synchronous Dispose: Microsoft DI throws for IAsyncDisposable-only services.
            if (scope.HasValue) scope.Value.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
