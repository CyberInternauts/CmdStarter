using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using com.cyberinternauts.csharp.CmdStarter.Samples.DevOpsTool.Options;
using com.cyberinternauts.csharp.CmdStarter.Samples.DevOpsTool.Services;
namespace com.cyberinternauts.csharp.CmdStarter.Samples.DevOpsTool.Commands;

[Command("service")]
public sealed class ServiceCommands(IDeploymentClient client)
{
    private static readonly string[] Environments = ["dev", "staging", "prod"];

    [Command]
    public async Task<int> DeployAsync(string name, DeployOptions options, [Option] string version = "latest")
    {
        var environment = options.Environment ?? "dev";
        if (!Environments.Contains(environment, StringComparer.Ordinal)) { Console.Error.WriteLine($"Unknown environment: {environment}"); return 2; }
        if (!client.KnownServices.Contains(name, StringComparer.Ordinal)) { Console.Error.WriteLine($"Unknown service: {name}"); return 3; }
        await client.DeployAsync(name, version, environment, options.DryRun);
        return 0;
    }

    [Command]
    public int Rollback(string name, DeployOptions options)
    {
        var environment = options.Environment ?? "dev";
        if (!Environments.Contains(environment, StringComparer.Ordinal)) { Console.Error.WriteLine($"Unknown environment: {environment}"); return 2; }
        if (!client.KnownServices.Contains(name, StringComparer.Ordinal)) { Console.Error.WriteLine($"Unknown service: {name}"); return 3; }
        client.Rollback(name, environment, options.DryRun);
        return 0;
    }
}
