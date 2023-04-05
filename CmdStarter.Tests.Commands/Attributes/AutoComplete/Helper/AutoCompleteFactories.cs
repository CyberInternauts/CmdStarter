using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete.Helper
{
    internal sealed class AutoCompleteOptionFactory : IAutoCompleteFactory
    {
        public const string DETAIL = " is a cool guy.";

        public string? GetDetail(string value) => value + DETAIL;

        public string? GetInsertText(string value) => value.ToUpper();

        public static IAutoCompleteFactory GetInstance() => new AutoCompleteOptionFactory();
    }

    internal sealed class AutoCompleteArgumentFactory : IAutoCompleteFactory
    {
        public const string DOCUMENTATION_EU = "Age of legal alcohol usage in the E.U.";
        public const string DOCUMENTATION_USA = "Age of legal alcohol usage in the U.S.A.";
        public const string DOCUMENTATION_NONE = "Cannot drink alcohol!";

        public string? GetDocumentation(string num)
        {
            int number = (int)Enum.Parse<AgeEnum>(num);

            if (number >= 21) return DOCUMENTATION_USA;

            if (number >= 18) return DOCUMENTATION_EU;

            return DOCUMENTATION_NONE;
        }

        public static IAutoCompleteFactory GetInstance() => new AutoCompleteArgumentFactory();
    }
}
