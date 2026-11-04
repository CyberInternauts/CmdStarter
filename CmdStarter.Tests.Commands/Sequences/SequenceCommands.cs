namespace CyberInternauts.CmdStarter.Tests.Commands.Sequences;

public static class SequenceLog
{
    public static List<string> Entries = new();
}

public sealed class Ok : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() =>
    {
        SequenceLog.Entries.Add("ok");
        return 0;
    });
}

public sealed class Fail : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() =>
    {
        SequenceLog.Entries.Add("fail");
        return 3;
    });
}

public sealed class Other : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() =>
    {
        SequenceLog.Entries.Add("other");
        return 0;
    });
}
