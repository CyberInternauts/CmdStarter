// Sample: plain commands. Two ordinary classes, with no base class and no interface, become commands because they carry [Command].

namespace CyberInternauts.CmdStarter.Samples.PlainCommand;

public static class Program
{
    public static Task<int> Main(string[] args) => new Starter().Start(args);
}
