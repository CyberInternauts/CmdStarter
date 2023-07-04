namespace com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces
{
    public interface IHasExpectedValue<ExpectedT>
    {
        ExpectedT ExpectedValue { get; }
    }
}
