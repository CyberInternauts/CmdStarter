// Sample: two top-level commands whose handler parameters become positional arguments.

namespace CyberInternauts.CmdStarter.Samples.TwoCommands;

public static class Program
{
    public static Task<int> Main(string[] args) => new Starter().Start(args);
}

