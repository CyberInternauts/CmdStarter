namespace com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces
{
    /// <summary>
    /// Provides an input for the REPL mode to parse.
    /// </summary>
    public interface IReplInputProvider
    {
        /// <summary>
        /// Returns an input that can be parsed by the command.
        /// </summary>
        public string GetInput();
    }
}
