namespace com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces
{
    public interface IErrorRunner
    {
        Type TypeOfException { get; }

        void ErrorInvoker();
    }
}
