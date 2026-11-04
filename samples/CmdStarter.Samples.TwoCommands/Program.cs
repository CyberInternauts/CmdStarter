// Sample: two top-level commands whose handler parameters become positional arguments.

using com.cyberinternauts.csharp.CmdStarter.Lib;

namespace com.cyberinternauts.csharp.CmdStarter.Samples.TwoCommands;

public static class Program
{
    public static Task<int> Main(string[] args) => new Starter().Start(args);
}

