using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions.Filters
{
    public sealed class GlobalOptionFilterByLackOfAttribute : IGlobalOptionsContainer, IOptByAttribute
    {
        public static string[] IncludedOptions => new string[] { INCLUDED_OPTION_1, INCLUDED_OPTION_2 };

        public static string[] ExcludedOptions => Array.Empty<string>();

        public int IncludedOption1 { get; set; }
        public const string INCLUDED_OPTION_1 = "included-option-1";

        public int IncludedOption2 { get; set; }
        public const string INCLUDED_OPTION_2 = "included-option-2";
    }
}
