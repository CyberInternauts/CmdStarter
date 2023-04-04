using com.cyberinternauts.csharp.CmdStarter.Lib.Extensions;
using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete.Helper
{
    internal sealed class AutoCompleteOptionFactory : IAutoCompleteFactory
    {
        public const string DETAIL = "Bob is a cool guy.";

        public string? GetDetail(string value) => DETAIL;

        public string? GetInsertText(string value) => value.ToUpper();

        public static IAutoCompleteFactory GetDefault() => new AutoCompleteOptionFactory();
    }

    internal sealed class AutoCompleteArgumentFactory : IAutoCompleteFactory
    {
        public const string DOCUMENTATION_EU = "Age of legal alcohol usage in the E.U.";
        public const string DOCUMENTATION_USA = "Age of legal alcohol usage in the U.S.A.";
        public const string DOCUMENTATION_NONE = "Cannot drink alcohol!";

        public string? GetDocumentation(string num)
        {
            if (int.Parse(num) >= 21) return DOCUMENTATION_USA;

            if (int.Parse(num) >= 18) return DOCUMENTATION_EU;

            return DOCUMENTATION_NONE;
        }

        public static IAutoCompleteFactory GetDefault() => new AutoCompleteArgumentFactory();
    }
}
