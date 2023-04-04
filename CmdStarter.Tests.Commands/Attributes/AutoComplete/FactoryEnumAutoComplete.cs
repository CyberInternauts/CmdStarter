using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete.Helper;
using System.CommandLine.Completions;
using System.CommandLine;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete
{
    public class FactoryEnumAutoComplete : StarterCommand, IHasAutoComplete
    {
        [AutoComplete<AutoCompleteOptionFactory>(typeof(NameEnum))]
        public string PersonName { get; set; } = null!;
        public const string OPTION_NAME = "person-name";

        public override Delegate MethodForHandling => Execute;

        private void Execute([AutoComplete<AutoCompleteArgumentFactory>(typeof(AgeEnum))] int age = 18)
        { }
        public const string ARGUMENT_NAME = "age";

        public IEnumerable<CompletionItem> OptionExpected()
        {
            Option<string> expected = new(OPTION_NAME);

            var instance = AutoCompleteOptionFactory.GetDefault();
            var items = new LinkedList<CompletionItem>();

            foreach (var name in Enum.GetNames<NameEnum>())
            {
                var item = new CompletionItem(
                    name,
                    detail: instance.GetDetail(name),
                    insertText: instance.GetInsertText(name));

                items.AddLast(item);
            }

            expected.AddCompletions((ctx) => items);

            return expected.GetCompletions();
        }

        public IEnumerable<CompletionItem> ArgumentExpected()
        {
            Argument<int> expected = new();

            var instance = AutoCompleteArgumentFactory.GetDefault();
            var items = new LinkedList<CompletionItem>();

            foreach (var name in Enum.GetNames<AgeEnum>())
            {
                var item = new CompletionItem(
                    name,
                    documentation: instance.GetDocumentation(name));

                items.AddLast(item);
            }

            expected.AddCompletions((ctx) => items);

            return expected.GetCompletions();
        }
    }
}
