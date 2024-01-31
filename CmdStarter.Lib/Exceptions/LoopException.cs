using System.Runtime.InteropServices;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions
{
    /// <summary>
    /// Loop detected exception
    /// </summary>
    public sealed class LoopException : Exception
    {
        /// <summary>
        /// First command in the loop
        /// </summary>
        public Type Command1 { get; private set; }

        /// <summary>
        /// Last command that loops back to the first one
        /// </summary>
        public Type Command2 { get; private set; }

        /// <summary>
        /// Constructor with both commands
        /// </summary>
        /// <param name="cmd1">First command in the loop</param>
        /// <param name="cmd2">Last command that loops back to the first one</param>
        public LoopException(Type cmd1,  Type cmd2) 
            : base("Commands tree loop between \"" + cmd1.FullName + "\" and \"" + cmd2.FullName + "\"")
        {
            Command1 = cmd1;
            Command2 = cmd2;
        }
    }
}
