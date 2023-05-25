namespace com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces
{
    /// <summary>
    /// Interface to implement command line execution of a class
    /// </summary>
    public interface IStarterCommand
    {
        /// <summary>
        /// Empty body method
        /// </summary>
        public readonly static Action EMPTY_EXECUTION = () => { };

        /// <summary>
        /// Get/set the global options manager
        /// </summary>
        public GlobalOptionsManager? GlobalOptionsManager { get; set; }

        /// <summary>
        /// Method that handles execution
        /// </summary>
        public Delegate HandlingMethod { get; }

        /// <summary>
        /// Get instance of the class implementing <see cref="IStarterCommand"/>
        /// </summary>
        /// <typeparam name="CommandType">Command type to get the instance of</typeparam>
        /// <returns>An instance of the class implementing <see cref="IStarterCommand"/></returns>
        public static virtual IStarterCommand GetInstance<CommandType>() where CommandType : IStarterCommand
        {
            return FactoryBag.RunFactory<CommandType>();
        }
    }
}
