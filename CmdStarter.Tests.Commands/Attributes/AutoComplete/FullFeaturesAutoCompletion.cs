using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete.Helper;
using System.CommandLine.Completions;
using System.CommandLine;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete
{
    public sealed class FullFeaturesAutoCompletion : StarterCommand, IHasAutoComplete
    {
        [AutoComplete<AutoCompleteOptionFullFeatures>()]
        public string PersonName { get; set; } = null!;
        public const string OPTION_NAME = "person-name";

        public override Delegate HandlingMethod => Execute;

        private void Execute([AutoComplete(typeof(AutoCompleteArgumentFullFeatures))] int age = 18)
        { }
        public const string ARGUMENT_NAME = "age";

        public IEnumerable<CompletionItem> OptionCompletionsExpected()
        {
            Option<string> expected = new(OPTION_NAME);

            var feeder = AutoCompletionOptionFeeder.GetInstance();
            var instance = AutoCompleteOptionFactory.GetInstance();
            var items = new LinkedList<CompletionItem>();

            foreach (var completion in feeder.GetAutoCompletes())
            {
                var item = new CompletionItem(
                    completion,
                    detail: instance.GetDetail(completion),
                    insertText: instance.GetInsertText(completion));

                items.AddLast(item);
            }

            expected.AddCompletions((ctx) => items);

            return expected.GetCompletions();
        }

        public IEnumerable<CompletionItem> ArgumentCompletionsExpected()
        {
            Argument<int> expected = new();

            var feeder = AutoCompletionArgumentFeeder.GetInstance();
            var instance = AutoCompleteArgumentFactory.GetInstance();
            var items = new LinkedList<CompletionItem>();

            foreach (var completion in feeder.GetAutoCompletes())
            {
                var item = new CompletionItem(
                    completion,
                    documentation: instance.GetDocumentation(completion));

                items.AddLast(item);
            }

            expected.AddCompletions((ctx) => items);

            return expected.GetCompletions();
        }
    }
}
