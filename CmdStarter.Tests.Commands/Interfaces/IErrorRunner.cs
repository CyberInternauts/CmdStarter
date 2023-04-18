namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces
{
    public interface IErrorRunner
    {
        Type TypeOfException { get; }

        void ErrorInvoker();
    }
}
