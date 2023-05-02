using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete.Helper;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;
using System.CommandLine;
using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete
{
    public class MergedIdenticalChoices : StarterCommand, IHasAutoComplete
    {

        [AutoComplete<NameEnum>()]
        [AutoComplete<NameEnum>()]
        public string? OptionWithDuplicates { get; set; }
        public const string OPTION_NAME = "option-with-duplicates";

        public override Delegate HandlingMethod => Execute;

        private void Execute(
            [AutoComplete<AgeEnum>()]
            [AutoComplete<AgeEnum>()]
            int age = 18)
        { }
        public const string ARGUMENT_NAME = "age";


        public IEnumerable<CompletionItem> OptionCompletionsExpected()
        {
            Option<string> expected = new(OPTION_NAME);

            expected.AddCompletions(Enum.GetNames(typeof(NameEnum)));

            return expected.GetCompletions();
        }


        public IEnumerable<CompletionItem> ArgumentCompletionsExpected()
        {
            Argument<int> expected = new();

            expected.AddCompletions(Enum.GetNames(typeof(AgeEnum)));

            return expected.GetCompletions();
        }
    }
}
