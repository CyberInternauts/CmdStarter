namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.DepencendyInjection
{
    public class ServiceManager : IServiceManager
    {
        private IDependentService service;

        public ServiceManager(IDependentService service)
        {
            this.service = service;
        }

        public object? GetService(Type serviceType)
        {
            return Activator.CreateInstance(serviceType, (object)service);
        }

        public void SetService(Type service)
        {
            // Useless in our test, as always create a new specific instance.
        }
    }
}
