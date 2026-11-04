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
        var error = Check(name, options.Environment);
        if (error != 0) return error;

        await client.DeployAsync(name, version, options.Environment, options.DryRun);
        return 0;
    }

    [Command]
    public int Rollback(string name, DeployOptions options)
    {
        var error = Check(name, options.Environment);
        if (error != 0) return error;

        client.Rollback(name, options.Environment, options.DryRun);
        return 0;
    }

    private int Check(string name, string environment)
    {
        if (!Environments.Contains(environment, StringComparer.Ordinal))
        {
            Console.Error.WriteLine($"Unknown environment: {environment}");
            return 2;
        }

        if (!client.KnownServices.Contains(name, StringComparer.Ordinal))
        {
            Console.Error.WriteLine($"Unknown service: {name}");
            return 3;
        }

        return 0;
    }
}

