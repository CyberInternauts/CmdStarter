namespace CyberInternauts.CmdStarter.Tests.Common.Interfaces;

public interface IHandleTester
{
    int ExpectedReturn { get; }

    List<HandlerData> ExpectedHandlerData { get; }

    List<HandlerData> ActualHandlerData { get; }
}
