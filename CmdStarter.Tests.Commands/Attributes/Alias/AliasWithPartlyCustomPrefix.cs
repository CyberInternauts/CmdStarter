using CyberInternauts.CmdStarter.Tests.Common.Interfaces;

namespace CyberInternauts.CmdStarter.Tests.Commands.Attributes.Alias;

public sealed class AliasWithPartlyCustomPrefix : StarterCommand, IHasAliases
{
    private const string COMMAND_NAME = "alias-with-partly-custom-prefix";

    private const string ALIAS = "custom";
    private const string PREFIX = "/";

    public string[] ExpectedOptionAliases => [OptionPrefix + OPTION_NAME, PREFIX + ALIAS, AliasAttribute.DefaultPrefix + ALIAS];

    public string[] ExpectedCommandAliases => [COMMAND_NAME];

    [Alias(true, ALIAS, Prefix = PREFIX)]
    [Alias(true, ALIAS)]
    public int Option { get; set; }
    public const string OPTION_NAME = "option";
}
