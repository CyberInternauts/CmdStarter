using System.CommandLine.Completions;
using System.CommandLine;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete.Helper;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete
{
    public sealed class LonelyEnumAutoComplete : StarterCommand, IHasAutoComplete
    {
        [AutoComplete<NameEnum>()]
        public string PersonName { get; set; } = null!;
        public const string OPTION_NAME = "person-name";

        public override Delegate HandlingMethod => Execute;

        private void Execute([AutoComplete<AgeEnum>()] int age = 18)
        { }
        public const string ARGUMENT_NAME = "age";

        public IEnumerable<CompletionItem> OptionCompletionsExpected()
        {
            Option<string> expected = new(OPTION_NAME);

            expected.CompletionSources.Add(Enum.GetNames(typeof(NameEnum)));

            return expected.GetCompletions(CompletionContext.Empty);
        }

        public IEnumerable<CompletionItem> ArgumentCompletionsExpected()
        {
            Argument<int> expected = new("age");

            expected.CompletionSources.Add(Enum.GetNames(typeof(AgeEnum)));

            return expected.GetCompletions(CompletionContext.Empty);
        }
    }
}
