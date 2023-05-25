namespace com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces
{
    /// <summary>
    /// Interface to obtain the list of completions
    /// </summary>
    public interface IAutoCompleteProvider
    {
        /// <summary>
        /// List of completions
        /// </summary>
        /// <returns></returns>
        string[] GetAutoCompletes();

        /// <summary>
        /// Obtain the instance of the auto completion provider
        /// </summary>
        /// <returns></returns>
        static abstract IAutoCompleteProvider GetInstance();
    }
}
