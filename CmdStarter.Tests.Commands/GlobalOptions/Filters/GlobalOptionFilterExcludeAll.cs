using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions.Filters
{
    [AllOptionsExcluded]
    public sealed class GlobalOptionFilterExcludeAll : IGlobalOptionsContainer, IOptByAttribute
    {
        public static string[] IncludedOptions => Array.Empty<string>();

        public static string[] ExcludedOptions => new string[] { NOT_INCLUDED_OPTION_1, NOT_INCLUDED_OPTION_2, NOT_INCLUDED_OPTION_3 };

        public int AllExcludeNotIncludedOption3 { get; set; }
        public const string NOT_INCLUDED_OPTION_3 = "all-exclude-not-included-option-3";

        public int AllExcludeNotIncludedOption1 { get; set; }
        public const string NOT_INCLUDED_OPTION_1 = "all-exclude-not-included-option-1";

        public int AllExcludeNotIncludedOption2 { get; set; }
        public const string NOT_INCLUDED_OPTION_2 = "all-exclude-not-included-option-2";
    }
}
