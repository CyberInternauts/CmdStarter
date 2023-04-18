using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Alias
{
    [Alias(COMMAND_ALIAS)]
    public sealed class OneAlias : StarterCommand, IHasAliases
    {
        private const string COMMAND_NAME = "one-alias";

        private const string COMMAND_ALIAS = "oa";
        private const string OPTION_ALIAS = "o";

        public string[] ExpectedOptionAliases => new string[] { OPTION_PREFIX + OPTION_NAME, AliasAttribute.DEFAULT_PREFIX + OPTION_ALIAS };
        public string[] ExpectedCommandAliases => new string[] { COMMAND_NAME, COMMAND_ALIAS };

        [Alias(true, OPTION_ALIAS)]
        public int Option { get; set; }
        public const string OPTION_NAME = "option";
    }
}
