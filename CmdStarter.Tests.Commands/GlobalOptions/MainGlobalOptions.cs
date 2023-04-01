using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions
{
    public class MainGlobalOptions : IGlobalOptionsContainer
    {
        [Description(INT_GLOBAL_OPTION_DESC)]
        public int IntGlobalOption { get; set; } = 888;
        public const string INT_GLOBAL_OPTION_KEBAB = "int-global-option";
        public const string INT_GLOBAL_OPTION_DESC = "My first global option";

        [Hidden]
        [Description(HIDDEN_INT_GLOBAL_OPTION_DESC)]
        public int HiddenIntGlobalOption { get; set; } = 887;
        public const string HIDDEN_INT_GLOBAL_OPTION_DESC = "Hidden global option";
    }
}
