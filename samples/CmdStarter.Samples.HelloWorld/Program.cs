// Sample: the smallest CmdStarter app. One class command, rooted because it is the only one.

namespace CyberInternauts.CmdStarter.Samples.HelloWorld;

public static class Program
{
    public static Task<int> Main(string[] args) => new Starter().Start(args);
}

