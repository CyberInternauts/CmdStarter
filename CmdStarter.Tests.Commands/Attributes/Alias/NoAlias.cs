using CyberInternauts.CmdStarter.Tests.Common.Interfaces;

namespace CyberInternauts.CmdStarter.Tests.Commands.Attributes.Alias;

public sealed class NoAlias : StarterCommand, IHasAliases
{
    private const string COMMAND_NAME = "no-alias";

    public string[] ExpectedOptionAliases => [OptionPrefix + OPTION_NAME];
    public string[] ExpectedCommandAliases => [COMMAND_NAME];

    public int Option { get; set; }
    public const string OPTION_NAME = "option";
}
