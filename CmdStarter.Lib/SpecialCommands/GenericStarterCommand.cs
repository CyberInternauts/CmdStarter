using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using System.Data;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.SpecialCommands
{
    public sealed class GenericStarterCommand<CommandType> : StarterCommand where CommandType : IStarterCommand
    {

        public Type IStarterCommandType { get; init; }

        public override Delegate HandlingMethod { get => UnderlyingCommand.HandlingMethod; }

        public override GlobalOptionsManager? GlobalOptionsManager
        {
            get => UnderlyingCommand.GlobalOptionsManager;
            set => UnderlyingCommand.GlobalOptionsManager = value;
        }

        internal GenericStarterCommand() : base(typeof(CommandType).Name)
        {
            IStarterCommandType = typeof(CommandType);

            // Get method from specific type implementing <see cref="IStarterCommand"> to get the object and this method prevents null already
            var method = IStarterCommandType.GetMethod(nameof(GetInstance))!.MakeGenericMethod(IStarterCommandType);
            UnderlyingCommand = (IStarterCommand)method.Invoke(null, null)!;
        }

    }
}
