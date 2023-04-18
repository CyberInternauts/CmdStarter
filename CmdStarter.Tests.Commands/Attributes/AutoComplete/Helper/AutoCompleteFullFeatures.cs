using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete.Helper
{
    internal class AutoCompleteOptionFullFeatures : IAutoCompleteFactory, IAutoCompleteProvider
    {
        public const string DETAIL = " is a cool guy.";

        public string? GetDetail(string value) => value + DETAIL;

        public string? GetInsertText(string value) => value.ToUpper();

        public static IAutoCompleteFactory GetInstance() => new AutoCompleteOptionFullFeatures();

        static IAutoCompleteProvider IAutoCompleteProvider.GetInstance() => (AutoCompleteOptionFullFeatures)GetInstance();

        public string[] GetAutoCompletes() => Enum.GetNames<NameEnum>();
    }

    internal class AutoCompleteArgumentFullFeatures : IAutoCompleteFactory, IAutoCompleteProvider
    {
        public static IAutoCompleteFactory GetInstance() => new AutoCompleteArgumentFullFeatures();

        static IAutoCompleteProvider IAutoCompleteProvider.GetInstance() => (AutoCompleteArgumentFullFeatures)GetInstance();

        public string[] GetAutoCompletes() => Enum.GetNames<AgeEnum>();

        public string? GetDocumentation(string num)
        {
            return AutoCompleteArgumentFactory.GetSharedDocumentation(num);
        }
    }
}
