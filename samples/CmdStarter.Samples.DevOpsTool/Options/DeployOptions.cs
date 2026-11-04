using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
namespace com.cyberinternauts.csharp.CmdStarter.Samples.DevOpsTool.Options;

public sealed class DeployOptions : IGlobalOptionsContainer
{
    public string Environment { get; set; } = "dev";
    public bool DryRun { get; set; }
}
