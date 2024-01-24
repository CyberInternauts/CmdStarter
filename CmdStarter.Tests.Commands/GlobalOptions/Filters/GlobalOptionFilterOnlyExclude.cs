using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions.Filters
{
    public sealed class GlobalOptionFilterOnlyExclude : IGlobalOptionsContainer, IOptByAttribute
    {
        public static string[] IncludedOptions => new string[] { INCLUDED_OPTION };

        public static string[] ExcludedOptions => new string[] { NOT_INCLUDED_OPTION_1, NOT_INCLUDED_OPTION_2 };

        public int OnlyExcludeIncludedOption { get; set; }
        public const string INCLUDED_OPTION = "only-exclude-included-option";

        [NotOption]
        public int OnlyExcludeNotIncludedOption1 { get; set; }
        public const string NOT_INCLUDED_OPTION_1 = "only-exclude-not-included-option-1";

        [NotOption]
        public int OnlyExcludeNotIncludedOption2 { get; set; }
        public const string NOT_INCLUDED_OPTION_2 = "only-exclude-not-included-option-2";
    }
}
