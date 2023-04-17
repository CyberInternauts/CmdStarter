using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete.Helper
{
    internal sealed class AutoCompletionArgumentFeeder : IAutoCompleteProvider
    {
        public string[] GetAutoCompletes() => FakeService().ToArray();

        private IEnumerable<string> FakeService()
        {
            return Enum.GetNames<AgeEnum>();
        }

        public static IAutoCompleteProvider GetInstance() => new AutoCompletionArgumentFeeder();
    }

    internal sealed class AutoCompletionOptionFeeder : IAutoCompleteProvider
    {
        public string[] GetAutoCompletes() => FakeService().ToArray();

        private IEnumerable<string> FakeService()
        {
            return Enum.GetNames<NameEnum>();
        }

        public static IAutoCompleteProvider GetInstance() => new AutoCompletionOptionFeeder();
    }
}
