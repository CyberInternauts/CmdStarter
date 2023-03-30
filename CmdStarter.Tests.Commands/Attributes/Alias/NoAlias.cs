using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Alias
{
    public sealed class NoAlias : StarterCommand, IHasAliases
    {
        private const string COMMAND_NAME = "no-alias";

        public string[] ExpectedOptionAliases => new string[] { OPTION_PREFIX + OPTION_NAME };
        public string[] ExpectedCommandAliases => new string[] { COMMAND_NAME };

        public int Option { get; set; }
        public const string OPTION_NAME = "option";
    }
}
