using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions
{
    public class SecondaryGlobalOptions : IGlobalOptionsContainer
    {
        public int SecondaryIntGlobalOption { get; set; } = 886;
        public const string SECONDARY_INT_GLOBAL_OPTION_KEBAB = "secondary-int-global-option";
    }
}
