using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete.Helper;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;
using System.CommandLine.Completions;
using System.CommandLine;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete
{
    public sealed class FactoryFeederAutoCompletion : StarterCommand, IHasAutoComplete
    {
        [AutoComplete<AutoCompleteOptionFactory>(typeof(AutoCompletionOptionFeeder))]
        public string PersonName { get; set; } = null!;
        public const string OPTION_NAME = "person-name";

        public override Delegate MethodForHandling => Execute;

        private void Execute([AutoComplete<AutoCompleteArgumentFactory>(typeof(AutoCompletionArgumentFeeder))] int age = 18)
        { }
        public const string ARGUMENT_NAME = "age";

        public IEnumerable<CompletionItem> OptionExpected()
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

        public IEnumerable<CompletionItem> ArgumentExpected()
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
