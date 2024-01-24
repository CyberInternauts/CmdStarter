using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions.Filters
{
    public sealed class GlobalOptionsFilterOnlyInclude : IGlobalOptionsContainer, IOptByAttribute
    {
        public static string[] IncludedOptions => new string[] { INCLUDED_OPTION };

        public static string[] ExcludedOptions => new string[] { NOT_INCLUDED_OPTION_1, NOT_INCLUDED_OPTION_2 };

        [Option]
        public int OnlyIncludeIncludedOption { get; set; }
        public const string INCLUDED_OPTION = "only-include-included-option";

        public int OnlyIncludeNotIncludedOption1 { get; set; }
        public const string NOT_INCLUDED_OPTION_1 = "only-include-not-included-option-1";

        public int OnlyIncludeNotIncludedOption2 { get; set; }
        public const string NOT_INCLUDED_OPTION_2 = "only-include-not-included-option-2";
    }
}
