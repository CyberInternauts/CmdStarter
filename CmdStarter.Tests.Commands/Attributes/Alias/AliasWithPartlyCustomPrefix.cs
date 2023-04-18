using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Alias
{
    public sealed class AliasWithPartlyCustomPrefix : StarterCommand, IHasAliases
    {
        private const string COMMAND_NAME = "alias-with-partly-custom-prefix";

        private const string ALIAS = "custom";
        private const string PREFIX = "/";

        public string[] ExpectedOptionAliases => new string[] { OPTION_PREFIX + OPTION_NAME, PREFIX + ALIAS, AliasAttribute.DEFAULT_PREFIX + ALIAS };

        public string[] ExpectedCommandAliases => new string[] { COMMAND_NAME };

        [Alias(true, ALIAS, Prefix = PREFIX)]
        [Alias(true, ALIAS)]
        public int Option { get; set; }
        public const string OPTION_NAME = "option";
    }
}
