namespace CyberInternauts.CmdStarter.Tests.Commands.DefaultValues;

public sealed class DefaultsGlobalOptions : IGlobalOptionsContainer
{
    public string Region { get; set; } = "region-default";
    public bool Verbose { get; set; } = true;
}
