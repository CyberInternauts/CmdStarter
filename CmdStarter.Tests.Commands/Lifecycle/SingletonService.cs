namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Lifecycle;

public sealed class SingletonService : IDisposable
{
    private bool isDisposed;
    public bool IsDisposed => isDisposed;
    public void Dispose() => isDisposed = true;
}
