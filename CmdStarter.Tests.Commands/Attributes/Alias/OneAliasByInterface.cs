using CyberInternauts.CmdStarter.Tests.Common.Interfaces;

namespace CyberInternauts.CmdStarter.Tests.Commands.Attributes.Alias;

[Alias(COMMAND_ALIAS)]
public sealed class OneAliasByInterface : IStarterCommand, IHasAliases
{
    private const string COMMAND_NAME = "one-alias-by-interface";

    private const string COMMAND_ALIAS = "oabi";
    private const string OPTION_ALIAS = "obi";

    public string[] ExpectedOptionAliases => [StarterCommand.OptionPrefix + OPTION_NAME, AliasAttribute.DefaultPrefix + OPTION_ALIAS];
    public string[] ExpectedCommandAliases => [COMMAND_NAME, COMMAND_ALIAS];

    [Alias(true, OPTION_ALIAS)]
    public int Option { get; set; }
    public const string OPTION_NAME = "option";

    public GlobalOptionsManager? GlobalOptionsManager { get; set; }

    public Delegate HandlingMethod => IStarterCommand.EmptyExecution;
}
