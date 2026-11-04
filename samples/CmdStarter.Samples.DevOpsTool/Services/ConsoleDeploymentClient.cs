namespace com.cyberinternauts.csharp.CmdStarter.Samples.DevOpsTool.Services;

public sealed class ConsoleDeploymentClient : IDeploymentClient
{
    public IReadOnlyList<string> KnownServices { get; } = ["api", "web", "worker"];
    public async Task DeployAsync(string service, string version, string environment, bool dryRun)
    {
        Console.Out.WriteLine($"{(dryRun ? "[dry-run] " : string.Empty)}Deploying {service} {version} to {environment}");
        await Task.Yield();
    }
    public void Rollback(string service, string environment, bool dryRun) => Console.Out.WriteLine($"{(dryRun ? "[dry-run] " : string.Empty)}Rolled back {service} on {environment}");
}
