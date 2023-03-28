using System.ComponentModel;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments
{
    public class FullArgs : StarterCommand
    {
        public override Delegate MethodForHandling => HandleExecution;

        public int MyOpt { get; set; } = 888;

        private void HandleExecution([Description("First param")] string param1, int param2, bool param3 = true)
        {
            Console.WriteLine("param1=" + param1 + "\n" + "param2=" + param2 + "\n" + "MyOpt=" + MyOpt);
        }
    }
}
