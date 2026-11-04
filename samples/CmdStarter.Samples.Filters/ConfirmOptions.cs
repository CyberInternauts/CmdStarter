using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Samples.Filters;

public sealed class ConfirmOptions : IGlobalOptionsContainer
{
    public bool Yes { get; set; }
}
