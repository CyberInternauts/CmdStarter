// Sample: two top-level commands whose handler parameters become positional arguments.

using CyberInternauts.CmdStarter;

namespace CyberInternauts.CmdStarter.Samples.TwoCommands;

public static class Program
{
    public static Task<int> Main(string[] args) => new Starter().Start(args);
}

