using com.cyberinternauts.csharp.CmdStarter.Lib;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Lifecycle;

public sealed class ThrowingCommand : StarterCommand, IDisposable
{
    public int Id { get; } = LifecycleLog.NextId();
    public override Delegate HandlingMethod => (Action)Execute;
    public void Execute() { LifecycleLog.Entries.Add($"execute:{Id}"); throw new InvalidOperationException("Lifecycle test failure"); }
    public void Dispose() => LifecycleLog.Entries.Add($"dispose:{Id}");
}
