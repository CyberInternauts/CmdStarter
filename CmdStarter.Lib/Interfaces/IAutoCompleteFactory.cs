namespace com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces
{
    public interface IAutoCompleteFactory
    {
        /// <summary>
        /// Gets the value used to sort the completion item in a list. If this is not provided, then <see cref="GetLabel"/> is used.
        /// </summary>
        string? GetSortText(string value) => null;

        /// <summary>
        /// Gets the text to be inserted by this completion item. If this is not provided, then <see cref="GetLabel"/> is used.
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

        static abstract IAutoCompleteFactory GetDefault();
    }
}