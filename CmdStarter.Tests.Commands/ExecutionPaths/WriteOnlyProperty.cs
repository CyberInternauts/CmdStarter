namespace CyberInternauts.CmdStarter.Tests.Commands.ExecutionPaths;

public sealed class WriteOnlyProperty : StarterCommand
{
    private int written;

    public int Readable { get; set; }

    // Settable but not readable.
    public int WriteOnly { set => written = value; }

    public int Written => written;

    public override Delegate HandlingMethod => (Func<int>)(() => Readable);
}
