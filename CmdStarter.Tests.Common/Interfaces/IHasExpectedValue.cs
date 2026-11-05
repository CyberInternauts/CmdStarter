namespace CyberInternauts.CmdStarter.Tests.Common.Interfaces;

public interface IHasExpectedValue<ExpectedT>
{
    ExpectedT ExpectedValue { get; }
}
