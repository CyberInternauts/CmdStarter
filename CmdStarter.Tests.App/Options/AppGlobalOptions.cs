using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.App.Options;

public sealed class AppGlobalOptions : IGlobalOptionsContainer
{
    public bool Verbose { get; set; }
}
