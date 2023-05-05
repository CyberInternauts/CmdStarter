using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Alias
{
    [Alias(COMMAND_ALIAS)]
    public sealed class OneAliasByInterface : IStarterCommand, IHasAliases
    {
        private const string COMMAND_NAME = "one-alias-by-interface";

        private const string COMMAND_ALIAS = "oabi";
        private const string OPTION_ALIAS = "obi";

        public string[] ExpectedOptionAliases => new string[] { StarterCommand.OPTION_PREFIX + OPTION_NAME, AliasAttribute.DEFAULT_PREFIX + OPTION_ALIAS };
        public string[] ExpectedCommandAliases => new string[] { COMMAND_NAME, COMMAND_ALIAS };

        [Alias(true, OPTION_ALIAS)]
        public int Option { get; set; }
        public const string OPTION_NAME = "option";

        public GlobalOptionsManager? GlobalOptionsManager { get; set; }

        public Delegate HandlingMethod => Execute;

        public void Execute()
        {

        }

        public static IStarterCommand GetInstance<CommandType>() where CommandType : IStarterCommand
            => new OneAliasByInterface();
    }
}
