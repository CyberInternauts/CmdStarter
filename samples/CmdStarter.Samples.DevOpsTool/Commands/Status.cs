using CyberInternauts.CmdStarter.Samples.DevOpsTool.Options;
using CyberInternauts.CmdStarter.Samples.DevOpsTool.Services;

namespace CyberInternauts.CmdStarter.Samples.DevOpsTool.Commands;

public sealed class Status(IDeploymentClient client) : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() =>
    {
        Console.Out.WriteLine($"Environment: {GetGlobalOptions<DeployOptions>()?.Environment ?? "dev"}");
        foreach (var service in client.KnownServices) Console.Out.WriteLine($"{service}: healthy");
        return 0;
    });
}

