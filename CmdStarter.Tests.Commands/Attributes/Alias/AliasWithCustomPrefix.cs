using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Alias
{
    public sealed class AliasWithCustomPrefix : StarterCommand, IHasAliases
    {
        private const string COMMAND_NAME = "alias-with-custom-prefix";

        private const string ALIAS = "custom";
        private const string PREFIX = "/";

        public string[] ExpectedOptionAliases => new string[] { OPTION_PREFIX + OPTION_NAME, PREFIX + ALIAS };

        public string[] ExpectedCommandAliases => new string[] { COMMAND_NAME };

        [Alias(true, ALIAS, Prefix = PREFIX)]
        public int Option { get; set; }
        public const string OPTION_NAME = "option";
    }
}
