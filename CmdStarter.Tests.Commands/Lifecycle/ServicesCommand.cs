namespace CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;

public sealed class ServicesCommand : StarterCommand, IDisposable
{
    private bool isDisposed;

    public ServicesCommand(SingletonService singleton, ScopedService scoped, ScopedConsumer consumer, TransientService transient1, TransientService transient2, AsyncScopedService asyncScoped)
    {
        Singleton = singleton;
        Scoped = scoped;
        Consumer = consumer;
        Transient1 = transient1;
        Transient2 = transient2;
        AsyncScoped = asyncScoped;
    }

    public SingletonService Singleton { get; }
    public ScopedService Scoped { get; }
    public ScopedConsumer Consumer { get; }
    public TransientService Transient1 { get; }
    public TransientService Transient2 { get; }
    public AsyncScopedService AsyncScoped { get; }
    public bool IsDisposed => isDisposed;
    public override Delegate HandlingMethod => (Func<int>)Execute;
    public int Execute() => 0;
    public void Dispose() => isDisposed = true;
}
