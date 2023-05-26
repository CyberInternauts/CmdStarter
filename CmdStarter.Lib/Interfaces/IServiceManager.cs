namespace com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces
{
    /// <summary>
    /// Interface to support dependency injection easily
    /// </summary>
    public interface IServiceManager : IServiceProvider
    {
        /// <summary>
        /// Add/set a service accessible through dependency injection
        /// </summary>
        /// <param name="service">The type of the service</param>
        public void SetService(Type service);
    }
}
