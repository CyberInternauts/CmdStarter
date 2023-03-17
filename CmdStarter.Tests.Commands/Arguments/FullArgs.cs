using System.ComponentModel;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments
{
    public class FullArgs : StarterCommand
    {
        public override Delegate MethodForHandling => HandleExecution;

        private void HandleExecution([Description("First param")] string param1, int param2, bool param3 = true)
        {
            
        }
    }
}
