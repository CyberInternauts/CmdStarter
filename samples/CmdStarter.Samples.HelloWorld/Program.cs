// Sample: the smallest CmdStarter app. One class command, rooted because it is the only one.

using com.cyberinternauts.csharp.CmdStarter.Lib;

namespace com.cyberinternauts.csharp.CmdStarter.Samples.HelloWorld;

public static class Program
{
    public static Task<int> Main(string[] args) => new Starter().Start(args);
}

