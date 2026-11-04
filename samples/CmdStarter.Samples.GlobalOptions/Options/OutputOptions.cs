using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Samples.GlobalOptions.Options;

public sealed class OutputOptions : IGlobalOptionsContainer
{
    public bool Verbose { get; set; }
}

