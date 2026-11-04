using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using com.cyberinternauts.csharp.CmdStarter.Samples.GlobalOptions.Options;
namespace com.cyberinternauts.csharp.CmdStarter.Samples.GlobalOptions.Commands;

public sealed class InfoCommands
{
    [Command]
    public static void Info(OutputOptions options)
    {
        Console.Out.WriteLine("info: v1");
        if (options.Verbose) Console.Out.WriteLine("details: build 42");
    }
}
