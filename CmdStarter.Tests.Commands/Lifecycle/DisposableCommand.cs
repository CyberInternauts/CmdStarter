using com.cyberinternauts.csharp.CmdStarter.Lib;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Lifecycle;

public sealed class DisposableCommand : StarterCommand, IDisposable
{
    public int Id { get; } = LifecycleLog.NextId();

    public override Delegate HandlingMethod => (Func<int>)Execute;

    public int Execute()
    {
        LifecycleLog.Entries.Add($"execute:{Id}");
        return Id;
    }

    public void Dispose() => LifecycleLog.Entries.Add($"dispose:{Id}");
}
