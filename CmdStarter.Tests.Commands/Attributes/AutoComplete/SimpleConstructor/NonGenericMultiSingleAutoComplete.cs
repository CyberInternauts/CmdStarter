using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;
using System.CommandLine;
using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete.SimpleConstructor
{
    public sealed class NonGenericMultiSingleAutoComplete : StarterCommand, IHasAutoComplete
    {
        private const string OPTION_COMPLETION_1 = "Bob";
        private const string OPTION_COMPLETION_2 = "Robert";

        private const string ARGUMENT_COMPLETION_1 = "18";
        private const string ARGUMENT_COMPLETION_2 = "60";

        [AutoComplete(OPTION_COMPLETION_1)]
        [AutoComplete(OPTION_COMPLETION_2)]
        public string PersonName { get; set; } = null!;
        public const string OPTION_NAME = "person-name";

        public override Delegate MethodForHandling => Execute;

        private void Execute(
            [AutoComplete(ARGUMENT_COMPLETION_1)]
            [AutoComplete(ARGUMENT_COMPLETION_2)]
            int age = 18)
        { }
        public const string ARGUMENT_NAME = "age";

        public IEnumerable<CompletionItem> OptionExpected()
        {
            Option<string> expected = new(OPTION_NAME);

            expected.AddCompletions(OPTION_COMPLETION_1, OPTION_COMPLETION_2);

            return expected.GetCompletions();
        }

        public IEnumerable<CompletionItem> ArgumentExpected()
        {
            Argument<int> expected = new();

            expected.AddCompletions(ARGUMENT_COMPLETION_1, ARGUMENT_COMPLETION_2);

            return expected.GetCompletions();
        }
    }
}
