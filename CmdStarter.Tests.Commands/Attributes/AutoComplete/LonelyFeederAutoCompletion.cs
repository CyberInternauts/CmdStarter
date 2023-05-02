using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete.Helper;
using System.CommandLine.Completions;
using System.CommandLine;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete
{
    public sealed class LonelyFeederAutoCompletion : StarterCommand, IHasAutoComplete
    {
        [AutoComplete<AutoCompletionOptionFeeder>()]
        public string PersonName { get; set; } = null!;
        public const string OPTION_NAME = "person-name";

        public override Delegate HandlingMethod => Execute;

        private void Execute([AutoComplete<AutoCompletionArgumentFeeder>()] int age = 18)
        { }
        public const string ARGUMENT_NAME = "age";

        public IEnumerable<CompletionItem> OptionCompletionsExpected()
        {
            Option<string> expected = new(OPTION_NAME);

            var instance = AutoCompletionOptionFeeder.GetInstance();

            expected.AddCompletions(instance.GetAutoCompletes());

            return expected.GetCompletions();
        }

        public IEnumerable<CompletionItem> ArgumentCompletionsExpected()
        {
            Argument<int> expected = new();

            var instance = AutoCompletionArgumentFeeder.GetInstance();

            expected.AddCompletions(instance.GetAutoCompletes());

            return expected.GetCompletions();
        }
    }
}
