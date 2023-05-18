namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.DepencendyInjection
{
    public class Dependent1 : IStarterCommand
    {
        private readonly IDependentService service;

        public Dependent1(IDependentService service) { this.service = service; }

        public GlobalOptionsManager? GlobalOptionsManager { get; set; }


        public Delegate HandlingMethod => () => service.GetInt();
    }
}
