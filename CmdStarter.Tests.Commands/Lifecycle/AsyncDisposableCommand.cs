using CyberInternauts.CmdStarter;

namespace CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;

public sealed class AsyncDisposableCommand : StarterCommand, IAsyncDisposable
{
    public int Id { get; } = LifecycleLog.NextId();
    public override Delegate HandlingMethod => (Func<int>)Execute;
    public int Execute() { LifecycleLog.Entries.Add($"execute:{Id}"); return Id; }
    public ValueTask DisposeAsync() { LifecycleLog.Entries.Add($"dispose-async:{Id}"); return ValueTask.CompletedTask; }
}
