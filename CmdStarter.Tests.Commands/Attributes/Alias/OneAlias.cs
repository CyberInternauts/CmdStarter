namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Alias
{
    public sealed class OneAlias : StarterCommand, IHasAliases
    {
        private const string Alias = "o";

        public string[] ExpectedAliases => new string[] { OPTION_PREFIX + OptionName, Alias };

        [Alias(Alias)]
        public int Option { get; set; }
        public const string OptionName = "option";
    }
}
