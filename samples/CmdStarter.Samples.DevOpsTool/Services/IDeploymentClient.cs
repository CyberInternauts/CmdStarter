namespace com.cyberinternauts.csharp.CmdStarter.Samples.DevOpsTool.Services;

public interface IDeploymentClient
{
    IReadOnlyList<string> KnownServices { get; }
    Task DeployAsync(string service, string version, string environment, bool dryRun);
    void Rollback(string service, string environment, bool dryRun);
}

