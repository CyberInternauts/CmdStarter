using System.Runtime.InteropServices;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions
{
    public class LoopException : Exception
    {
        public Type Command1 { get; private set; }
        public Type Command2 { get; private set; }

        public LoopException(Type cmd1,  Type cmd2) 
            : base("Commands tree loop between \"" + cmd1.FullName + "\" and \"" + cmd2.FullName + "\"")
        {
            Command1 = cmd1;
            Command2 = cmd2;
        }
    }
}
