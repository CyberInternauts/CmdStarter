namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Alias
{
    public sealed class NoAlias : StarterCommand, IHasAliases
    {
        public string[] ExpectedAliases => new string[] { OPTION_PREFIX + OptionName };

        public int Option { get; set; }
        public const string OptionName = "option";
    }
}
