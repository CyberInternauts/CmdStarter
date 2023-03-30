using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Alias
{
    [Alias(COMMAND_ALIAS_1, COMMAND_ALIAS_2, COMMAND_ALIAS_3)]
    public sealed class MultiAlias : StarterCommand, IHasAliases
    {
        private const string COMMAND_NAME = "multi-alias";

        private const string COMMAND_ALIAS_1 = "ma";
        private const string COMMAND_ALIAS_2 = "mu-al";
        private const string COMMAND_ALIAS_3 = "multi-al";

        private const string OPTION_ALIAS_1 = "o";
        private const string OPTION_ALIAS_2 = "op";
        private const string OPTION_ALIAS_3 = "opt";

        public string[] ExpectedOptionAliases => new string[]
        {
            OPTION_PREFIX + OPTION_NAME,
            AliasAttribute.DEFAULT_PREFIX + OPTION_ALIAS_1,
            AliasAttribute.DEFAULT_PREFIX + OPTION_ALIAS_2,
            AliasAttribute.DEFAULT_PREFIX + OPTION_ALIAS_3
        };

        public string[] ExpectedCommandAliases => new string[]
        {
            COMMAND_NAME,
            COMMAND_ALIAS_1,
            COMMAND_ALIAS_2,
            COMMAND_ALIAS_3
        };

        [Alias(true, OPTION_ALIAS_1, OPTION_ALIAS_2, OPTION_ALIAS_3)]
        public int Option { get; set; }
        public const string OPTION_NAME = "option";
    }
}
