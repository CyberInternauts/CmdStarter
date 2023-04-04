using NUnit.Framework;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces
{
    public interface IErrorRunner
    {
        TestDelegate ErrorRunner { get; }
    }
}
