using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Samples.DevOpsTool.Options;

public sealed class DeployOptions : IGlobalOptionsContainer
{
    public string Environment { get; set; } = "dev";

    public bool DryRun { get; set; }
}

