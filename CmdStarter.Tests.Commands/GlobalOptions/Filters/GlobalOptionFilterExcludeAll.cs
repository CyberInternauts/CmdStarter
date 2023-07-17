using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions
{
    [AllOptionsExcluded]
    public sealed class GlobalOptionFilterExcludeAll : IGlobalOptionsContainer, IOptByAttribute
    {
        public static string[] IncludedOptions => Array.Empty<string>();

        public static string[] ExcludedOptions => new string[] { NOT_INCLUDED_OPTION_1, NOT_INCLUDED_OPTION_2, NOT_INCLUDED_OPTION_3 };

        public int NotIncludedOption3 { get; set; }
        public const string NOT_INCLUDED_OPTION_3 = "not-included-option-3";

        public int NotIncludedOption1 { get; set; }
        public const string NOT_INCLUDED_OPTION_1 = "not-included-option-1";

        public int NotIncludedOption2 { get; set; }
        public const string NOT_INCLUDED_OPTION_2 = "not-included-option-2";
    }
}
