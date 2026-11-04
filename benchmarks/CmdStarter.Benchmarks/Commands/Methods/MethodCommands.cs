using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Benchmarks.Commands.Methods;

// Twelve method commands in two classes: six top-level commands and six in the "tools" group.

public class ReportCommands
{
    [Command]
    public int Summarize(string target) => 0;

    [Command]
    public int Export(string target) => 0;

    [Command]
    public int Import(string target) => 0;

    [Command]
    public int Verify(string target) => 0;

    [Command]
    public int Cleanup(string target) => 0;

    [Command]
    public int Archive(string target) => 0;
}

[Command("tools")]
public class ToolCommands
{
    [Command]
    public int Build(string target) => 0;

    [Command]
    public int Clean(string target) => 0;

    [Command]
    public int Pack(string target) => 0;

    [Command]
    public int Publish(string target) => 0;

    [Command]
    public int Restore(string target) => 0;

    [Command]
    public int Inspect(string target) => 0;
}
