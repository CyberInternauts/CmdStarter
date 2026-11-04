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

// Returns the code of a cancelled command (130): the sequence stops on it.
public sealed class Cancelled : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() =>
    {
        SequenceLog.Entries.Add("cancelled");
        return 130;
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

public sealed class Emit : StarterCommand
{
    public override Delegate HandlingMethod => (Func<CommandConsole, int>)(console =>
    {
        SequenceLog.Entries.Add("emit");
        console.Out.WriteLine("b");
        console.Out.WriteLine("a");
        console.Out.WriteLine("c");
        return 0;
    });
}

public sealed class Sort : StarterCommand
{
    public override Delegate HandlingMethod => (Func<CommandConsole, int>)(console =>
    {
        SequenceLog.Entries.Add("sort");
        foreach (var line in ReadLines(console).Order(StringComparer.Ordinal))
        {
            console.Out.WriteLine(line);
        }

        return 0;
    });

    internal static List<string> ReadLines(CommandConsole console)
    {
        var lines = new List<string>();
        while (console.In.ReadLine() is { } line)
        {
            lines.Add(line);
        }

        return lines;
    }
}

public sealed class Count : StarterCommand
{
    public override Delegate HandlingMethod => (Func<CommandConsole, int>)(console =>
    {
        SequenceLog.Entries.Add("count");
        var count = Sort.ReadLines(console).Count(line => line.Length > 0);
        console.Out.WriteLine(count);
        return count;
    });
}

public sealed class Warn : StarterCommand
{
    public override Delegate HandlingMethod => (Func<CommandConsole, int>)(console =>
    {
        SequenceLog.Entries.Add("warn");
        console.Error.WriteLine("warning");
        console.Out.WriteLine("x");
        return 0;
    });
}
