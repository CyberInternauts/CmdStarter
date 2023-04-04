using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete.Helper;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;
using System.CommandLine.Completions;
using System.CommandLine;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete
{
    public sealed class LonelyFeederAutoCompletion : StarterCommand, IHasAutoComplete
    {
        [AutoComplete<AutoCompletionOptionFeeder>()]
        public string PersonName { get; set; } = null!;
        public const string OPTION_NAME = "person-name";

        public override Delegate MethodForHandling => Execute;

        private void Execute([AutoComplete<AutoCompletionArgumentFeeder>()] int age = 18)
        { }
        public const string ARGUMENT_NAME = "age";

        public IEnumerable<CompletionItem> OptionExpected()
        {
            Option<string> expected = new(OPTION_NAME);

            var instance = AutoCompletionOptionFeeder.GetDefault();

            expected.AddCompletions(instance.GetAutoCompletes());

            return expected.GetCompletions();
        }

        public IEnumerable<CompletionItem> ArgumentExpected()
        {
            Argument<int> expected = new();

            var instance = AutoCompletionArgumentFeeder.GetDefault();

            expected.AddCompletions(instance.GetAutoCompletes());

            return expected.GetCompletions();
        }
    }
}
