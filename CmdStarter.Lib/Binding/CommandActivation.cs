using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Binding;

internal sealed class CommandActivation : IDisposable, IAsyncDisposable
{
    private readonly bool ownsInstance;
    private bool disposed;

    internal CommandActivation(IStarterCommand instance, bool ownsInstance)
    {
        Instance = instance;
        this.ownsInstance = ownsInstance;
    }

    internal IStarterCommand Instance { get; }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        if (!ownsInstance) return;
        if (Instance is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (Instance is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (!ownsInstance) return;
        if (Instance is IDisposable disposable)
        {
            disposable.Dispose();
        }
        else if (Instance is IAsyncDisposable asyncDisposable)
        {
            // InstantiateCommands is synchronous and public, so async-only instances must be synchronously drained here.
            asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
