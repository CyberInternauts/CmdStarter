namespace CyberInternauts.CmdStarter.Samples.ScopedServices.Services;

public sealed class UnitOfWork : IDisposable
{
    public void Commit() => Console.Out.WriteLine("committed");

    public void Dispose() => Console.Out.WriteLine("unit of work disposed");
}

