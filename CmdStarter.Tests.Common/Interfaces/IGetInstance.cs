namespace com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces
{
    public interface IGetInstance<T> where T : IGetInstance<T>
    {
        static abstract T GetInstance();
    }
}
