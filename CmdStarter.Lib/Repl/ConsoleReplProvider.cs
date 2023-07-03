using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Repl
{
    /// <summary>
    /// Provides command inputs from the default console.
    /// </summary>
    public sealed class ConsoleReplProvider : IReplInputProvider
    {
        /// <summary>
        /// Returns an input from the default console.
        /// </summary>
        /// <returns>A <see cref="string"/> with <see cref="Console.ReadLine"/>.</returns>
        public string GetInput()
        {
            return Console.ReadLine() ?? string.Empty;
        }
    }
}
