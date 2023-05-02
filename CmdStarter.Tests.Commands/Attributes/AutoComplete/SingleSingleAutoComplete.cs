using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;
using System.CommandLine;
using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete
{
    public sealed class SingleSingleAutoComplete : StarterCommand, IHasAutoComplete
    {
        private const string OPTION_COMPLETION_1 = "Bob";

        private const string ARGUMENT_COMPLETION_1 = "18";

        [AutoComplete(OPTION_COMPLETION_1)]
        public string PersonName { get; set; } = null!;
        public const string OPTION_NAME = "person-name";

        public override Delegate HandlingMethod => Execute;

        private void Execute([AutoComplete(ARGUMENT_COMPLETION_1)] int age = 18)
        { }
        public const string ARGUMENT_NAME = "age";

        public IEnumerable<CompletionItem> OptionCompletionsExpected()
        {
            Option<string> expected = new(OPTION_NAME);

            expected.AddCompletions(OPTION_COMPLETION_1);

            return expected.GetCompletions();
        }

        public IEnumerable<CompletionItem> ArgumentCompletionsExpected()
        {
            Argument<int> expected = new();

            expected.AddCompletions(ARGUMENT_COMPLETION_1);

            return expected.GetCompletions();
        }
    }
}
