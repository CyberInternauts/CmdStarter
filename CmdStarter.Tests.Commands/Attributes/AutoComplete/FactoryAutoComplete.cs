using com.cyberinternauts.csharp.CmdStarter.Lib.Extensions;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;
using System.CommandLine;
using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete
{
    public sealed class FactoryAutoComplete : StarterCommand, IHasAutoComplete
    {
        private const string OPTION_COMPLETION_1 = "BobOrFrank";

        private const int ARGUMENT_COMPLETION_1 = 18;

        [AutoComplete<string, AutoCompleteOptionFactory>(OPTION_COMPLETION_1)]
        public string PersonName { get; set; } = null!;
        public const string OPTION_NAME = "person-name";

        public override Delegate MethodForHandling => Execute;

        private void Execute([AutoComplete<int, AutoCompleteArgumentFactory>(ARGUMENT_COMPLETION_1)] int age = 18)
        { }
        public const string ARGUMENT_NAME = "age";

        public IEnumerable<CompletionItem> OptionExpected()
        {
            Option<string> expected = new(OPTION_NAME);

            var item = new CompletionItem(OPTION_COMPLETION_1.PascalToKebabCase());

            expected.AddCompletions((ctx) => new CompletionItem[] { item });

            return expected.GetCompletions();
        }

        public IEnumerable<CompletionItem> ArgumentExpected()
        {
            Argument<int> expected = new();

            var item = new CompletionItem(
                ARGUMENT_COMPLETION_1.ToString(),
                documentation: AutoCompleteArgumentFactory.DOCUMENTATION_EU);

            expected.AddCompletions((ctx) => new CompletionItem[] { item });

            return expected.GetCompletions();
        }
    }
}
