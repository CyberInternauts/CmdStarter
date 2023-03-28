namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Alias
{
    public sealed class MultiAlias : StarterCommand, IHasAliases
    {
        private const string Alias1 = "o";
        private const string Alias2 = "op";
        private const string Alias3 = "opt";


        public string[] ExpectedAliases => new string[] { OPTION_PREFIX + OptionName, Alias1, Alias2, Alias3 };

        [Alias(Alias1)]
        [Alias(Alias2)]
        [Alias(Alias3)]
        public int Option { get; set; }
        public const string OptionName = "option";
    }
}
