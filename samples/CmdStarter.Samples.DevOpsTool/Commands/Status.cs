using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Samples.DevOpsTool.Options;
using com.cyberinternauts.csharp.CmdStarter.Samples.DevOpsTool.Services;
namespace com.cyberinternauts.csharp.CmdStarter.Samples.DevOpsTool.Commands;

public sealed class Status(IDeploymentClient client) : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() =>
    {
        Console.Out.WriteLine($"Environment: {GetGlobalOptions<DeployOptions>()?.Environment ?? "dev"}");
        foreach (var service in client.KnownServices) Console.Out.WriteLine($"{service}: healthy");
        return 0;
    });
}
