using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options
{
    public class OptByInterface : IStarterCommand
    {
        public int IntOpt { get; set; } = 111;
        public const string INT_OPT_KEBAB = "int-opt";

        public GlobalOptionsManager? GlobalOptionsManager { get; set; }

        public Delegate HandlingMethod => Execute;

        public void Execute()
        {

        }

        public static IStarterCommand GetInstance<CommandType>() where CommandType : IStarterCommand
            => new OptByInterface();
    }
}
