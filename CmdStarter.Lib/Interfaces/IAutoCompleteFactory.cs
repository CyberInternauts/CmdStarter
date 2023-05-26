namespace com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces
{
    /// <summary>
    /// Interface to implement auto completion values options
    /// </summary>
    public interface IAutoCompleteFactory
    {
        /// <summary>
        /// Gets the value used to sort the completion item in a list. If this is not provided, then label is used.
        /// </summary>
        string? GetSortText(string value) => null;

        /// <summary>
        /// Gets the text to be inserted by this completion item. If this is not provided, then label is used.
        /// </summary>
        string? GetInsertText(string value) => null;

        /// <summary>
        /// Gets documentation about the completion item.
        /// </summary>
        string? GetDocumentation(string value) => null;

        /// <summary>
        /// Generates additional details regarding the completion item.
        /// </summary>
        string? GetDetail(string value) => null;

        /// <summary>
        /// Obtain the instance of the factory
        /// </summary>
        /// <returns></returns>
        static abstract IAutoCompleteFactory GetInstance();
    }
}