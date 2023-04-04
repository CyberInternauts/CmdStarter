using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces
{
    public interface IAutoCompleteFactory<T>
    {
        /// <summary>
        /// Gets the label value, which is the text displayed to users and, unless <see cref="GetInsertText"/> is set, is also used to populate the <see cref="CompletionItem.InsertText"/> property.
        /// </summary>
        string GetLabel(T value);

        /// <summary>
        /// Gets the value used to sort the completion item in a list. If this is not provided, then <see cref="GetLabel"/> is used.
        /// </summary>
        string? GetSortText(T value) => null;

        /// <summary>
        /// Gets the text to be inserted by this completion item. If this is not provided, then <see cref="GetLabel"/> is used.
        /// </summary>
        string? GetInsertText(T value) => null;

        /// <summary>
        /// Gets documentation about the completion item.
        /// </summary>
        string? GetDocumentation(T value) => null;

        /// <summary>
        /// Generates additional details regarding the completion item.
        /// </summary>
        string? GetDetail(T value) => null;

        static abstract IAutoCompleteFactory<T> GetDefault();
    }
}