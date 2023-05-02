namespace com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces
{
    public interface IHandleTester
    {
        int ExpectedReturn { get; }

        List<HandlerData> ExpectedHandlerData { get; }

        List<HandlerData> ActualHandlerData { get; }
    }
}
