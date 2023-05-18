namespace com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces
{
    public interface IServiceManager : IServiceProvider
    {
        public void SetService(Type service);
    }
}
