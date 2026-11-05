using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Tests.App.Options;

public sealed class AppGlobalOptions : IGlobalOptionsContainer
{
    public bool Verbose { get; set; }
}
