using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;
using System.CommandLine;
using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete
{
    public sealed class GenericEnumAutoComplete : StarterCommand, IHasAutoComplete
    {
        [AutoComplete<NameEnum>()]
        public NameEnum PersonName { get; set; }
        public const string OPTION_NAME = "person-name";

        public override Delegate MethodForHandling => Execute;

        private void Execute([AutoComplete<AgeEnum>()]AgeEnum age = AgeEnum.EUAdult)
        { }
        public const string ARGUMENT_NAME = "age";

        public IEnumerable<CompletionItem> OptionExpected()
        {
            Option<NameEnum> expected = new(OPTION_NAME);

            expected.AddCompletions(Enum.GetNames<NameEnum>());

            return expected.GetCompletions();
        }

        public IEnumerable<CompletionItem> ArgumentExpected()
        {
            Argument<AgeEnum> expected = new();

            expected.AddCompletions(Enum.GetNames<AgeEnum>());

            return expected.GetCompletions();
        }
    }
}
