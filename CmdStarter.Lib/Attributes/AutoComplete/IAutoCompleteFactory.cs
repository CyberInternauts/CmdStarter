namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes.AutoComplete
{
    public interface IAutoCompleteFactory<T>
    {
        /// <summary>
        /// Generates the label value, which is the text displayed to users and, unless <see cref="InsertTextFactory"/> is set, is also used to populate the <see cref="CompletionItem.InsertText"/> property.
        /// </summary>
        Func<T, string> LabelFactory { get; init; }

        /// <summary>
        /// Genetares the value used to sort the completion item in a list. If this is not provided, then <see cref="LabelFactory"/> is used.
        /// </summary>
        Func<T, string?> SortTextFactory { get => AutoCompleteAttribute<T>.NullFactory; }

        /// <summary>
        /// Generates the text to be inserted by this completion item. If this is not provided, then <see cref="LabelFactory"/> is used.
        /// </summary>
        Func<T, string?> InsertTextFactory { get => AutoCompleteAttribute<T>.NullFactory; }

        /// <summary>
        /// Generates documentation about the completion item.
        /// </summary>
        Func<T, string?> DocumentationFactory { get => AutoCompleteAttribute<T>.NullFactory; }

        /// <summary>
        /// Generates additional details regarding the completion item.
        /// </summary>
        Func<T, string?> DetailFactory { get => AutoCompleteAttribute<T>.NullFactory; }

        static abstract IAutoCompleteFactory<T> GetDefault();
    }
}