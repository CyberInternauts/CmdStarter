using com.cyberinternauts.csharp.CmdStarter.Lib.Extensions;
using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete
{
    internal sealed class AutoCompleteOptionFactory : IAutoCompleteFactory<string>
    {
        public const string DETAIL = "Bob is a cool guy.";

        public string GetLabel(string value) => value.PascalToKebabCase();

        public string? GetDetail(string value) => DETAIL;

        public string? GetInsertText(string value) => value.ToUpper();

        public static IAutoCompleteFactory<string> GetDefault() => new AutoCompleteOptionFactory();
    }

    internal sealed class AutoCompleteArgumentFactory : IAutoCompleteFactory<int>
    {
        public const string DOCUMENTATION_EU = "Age of legal alcohol usage in the E.U.";
        public const string DOCUMENTATION_USA = "Age of legal alcohol usage in the U.S.A.";
        public const string DOCUMENTATION_NONE = "Cannot drink alcohol!";

        public string GetLabel(int num) => num.ToString();

        public string? GetDocumentation(int num)
        {
            if (num >= 21) return DOCUMENTATION_USA;

            if (num >= 18) return DOCUMENTATION_EU;

            return DOCUMENTATION_NONE;
        }

        public static IAutoCompleteFactory<int> GetDefault() => new AutoCompleteArgumentFactory();
    }
}
