using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;
using System.CommandLine;
using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete
{
    public sealed class NonGenericMultiMultiAutoComplete : StarterCommand, IHasAutoComplete
    {
        private const string OPTION_COMPLETION_1 = "Bob";
        private const string OPTION_COMPLETION_2 = "Robert";
        private const string OPTION_COMPLETION_3 = "Rob";
        private const string OPTION_COMPLETION_4 = "Robby";

        private const string ARGUMENT_COMPLETION_1 = "18";
        private const string ARGUMENT_COMPLETION_2 = "60";
        private const string ARGUMENT_COMPLETION_3 = "21";
        private const string ARGUMENT_COMPLETION_4 = "6";

        [AutoComplete(OPTION_COMPLETION_1, OPTION_COMPLETION_2)]
        [AutoComplete(OPTION_COMPLETION_3, OPTION_COMPLETION_4)]
        public string PersonName { get; set; } = null!;
        public const string OPTION_NAME = "person-name";

        public override Delegate MethodForHandling => Execute;

        private void Execute(
            [AutoComplete(ARGUMENT_COMPLETION_1, ARGUMENT_COMPLETION_2)]
            [AutoComplete(ARGUMENT_COMPLETION_3, ARGUMENT_COMPLETION_4)]
            int age = 18)
        { }
        public const string ARGUMENT_NAME = "age";

        public IEnumerable<CompletionItem> OptionExpected()
        {
            Option<string> expected = new(OPTION_NAME);

            expected.AddCompletions(
                OPTION_COMPLETION_1, OPTION_COMPLETION_2,
                OPTION_COMPLETION_3, OPTION_COMPLETION_4);

            return expected.GetCompletions();
        }

        public IEnumerable<CompletionItem> ArgumentExpected()
        {
            Argument<int> expected = new();

            expected.AddCompletions(
                ARGUMENT_COMPLETION_1, ARGUMENT_COMPLETION_2,
                ARGUMENT_COMPLETION_3, ARGUMENT_COMPLETION_4);

            return expected.GetCompletions();
        }
    }
}
