namespace CyberInternauts.CmdStarter.Tests.Common.Interfaces;

public interface IHasAliases
{
    string[] ExpectedOptionAliases { get; }
    string[] ExpectedCommandAliases { get; }
}
