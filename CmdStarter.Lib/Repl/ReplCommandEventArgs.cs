using System.CommandLine;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Repl
{
    /// <summary>
    /// Event arguments for a REPL command execution event.
    /// </summary>
    public sealed class ReplCommandEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplCommandEventArgs"/> class.
        /// </summary>
        /// <param name="returnCode">The return code of the command execution.</param>
        public ReplCommandEventArgs(int returnCode)
        {
            Command = command;
            ReturnCode = returnCode;
        }

        /// <summary>
        /// Gets the return code of the command execution.
        /// </summary>
        public int ReturnCode { get; }
    }
}
