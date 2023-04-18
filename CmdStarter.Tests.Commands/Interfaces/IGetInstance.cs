namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces
{
    public interface IGetInstance<T> where T : IGetInstance<T>
    {
        static abstract T GetInstance();
    }
}
