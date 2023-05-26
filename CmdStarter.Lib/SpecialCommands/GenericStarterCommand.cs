using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using static com.cyberinternauts.csharp.CmdStarter.Lib.Reflection.Helper;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.SpecialCommands
{
    /// <summary>
    /// Facade for <see cref="IStarterCommand" /> that are not derived from <see cref="StarterCommand"/>
    /// </summary>
    /// <typeparam name="CommandType">Type of the underlying command</typeparam>
    public sealed class GenericStarterCommand<CommandType> : StarterCommand where CommandType : IStarterCommand
    {

        /// <summary>
        /// Get the underlying command type
        /// </summary>
        public Type IStarterCommandType { get; init; }

        /// <summary>
        /// Proxy for the underlying <see cref="IStarterCommand.HandlingMethod"/>
        /// </summary>
        public override Delegate HandlingMethod { get => UnderlyingCommand.HandlingMethod; }

        /// <summary>
        /// Proxy for the underlying <see cref="IStarterCommand.GlobalOptionsManager"/>
        /// </summary>
        public override GlobalOptionsManager? GlobalOptionsManager
        {
            get => UnderlyingCommand.GlobalOptionsManager;
            set => UnderlyingCommand.GlobalOptionsManager = value;
        }

        internal GenericStarterCommand() : base(typeof(CommandType).Name)
        {
            IStarterCommandType = typeof(CommandType);

            // Get method from specific type implementing <see cref="IStarterCommand"> to get the object and this method prevents null already
            var method = FindGetInstanceMethod(IStarterCommandType);
            UnderlyingCommand = (IStarterCommand)method.Invoke(null, null)!;
        }

    }
}
