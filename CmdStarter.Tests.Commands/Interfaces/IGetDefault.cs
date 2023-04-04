namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces
{
    public interface IGetDefault<T> where T : IGetDefault<T>
    {
        static abstract T GetDefault();
    }
}
