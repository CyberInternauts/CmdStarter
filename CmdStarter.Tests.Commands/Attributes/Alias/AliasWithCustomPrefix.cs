namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Alias
{
    public sealed class AliasWithCustomPrefix : StarterCommand, IHasAliases
    {
        private const string Alias = "o";
        private const string Prefix = "/";

        public string[] ExpectedAliases => new string[] { OPTION_PREFIX + OptionName, Prefix + Alias };

        [Alias(Alias, Prefix)]
        public int Option { get; set; }
        public const string OptionName = "option";
    }
}
