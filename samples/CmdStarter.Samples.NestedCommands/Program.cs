// Sample: nested commands, from the namespace hierarchy (remote) and from [Parent<T>] (config).

using com.cyberinternauts.csharp.CmdStarter.Lib;

namespace com.cyberinternauts.csharp.CmdStarter.Samples.NestedCommands;

public static class Program
{
    public static Task<int> Main(string[] args) => new Starter().Start(args);
}

