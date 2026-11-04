using com.cyberinternauts.csharp.CmdStarter.Lib;
namespace com.cyberinternauts.csharp.CmdStarter.Samples.HelloWorld;

public static class Program
{
    public static Task<int> Main(string[] args) => new Starter().Start(args);
}
