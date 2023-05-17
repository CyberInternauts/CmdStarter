namespace com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces
{
    public interface IStarterCommand
    {
        public readonly static Action EMPTY_EXECUTION = () => { };

        public GlobalOptionsManager? GlobalOptionsManager { get; set; }

        public Delegate HandlingMethod { get; }

        public static virtual IStarterCommand GetInstance<CommandType>() where CommandType : IStarterCommand
        {
            return FactoryBag.RunFactory<CommandType>();
        }
    }
}
