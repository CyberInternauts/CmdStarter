namespace CyberInternauts.CmdStarter.Tests.Common.Interfaces;

public interface IErrorRunner
{
    Type TypeOfException { get; }

    void ErrorInvoker();
}
