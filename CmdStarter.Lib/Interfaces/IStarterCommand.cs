namespace com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces
{
    public interface IStarterCommand
    {
        public GlobalOptionsManager? GlobalOptionsManager { get; set; }

        public Delegate HandlingMethod { get; }
    }
}
