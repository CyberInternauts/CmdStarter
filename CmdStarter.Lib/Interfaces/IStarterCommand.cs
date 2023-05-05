namespace com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces
{
    public interface IStarterCommand
    {
        public GlobalOptionsManager? GlobalOptionsManager { get; set; }

        public Delegate HandlingMethod { get; }

        public static abstract IStarterCommand GetInstance<CommandType>() where CommandType : IStarterCommand;
    }
}
