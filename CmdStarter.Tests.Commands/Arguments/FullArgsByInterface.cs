using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Alias;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments
{
    public class FullArgsByInterface : IStarterCommand
    {

        public int MyOpt { get; set; } = 888;

        public GlobalOptionsManager? GlobalOptionsManager { get; set; }

        public Delegate HandlingMethod => HandleExecution;

        private void HandleExecution([Description("First param")] string param1, int param2, bool param3 = true)
        {
            Console.WriteLine("param1=" + param1 + "\n" + "param2=" + param2 + "\n" + "MyOpt=" + MyOpt);
        }
    }
}
