using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes.AutoComplete;
using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
    public class AutoCompleteAttribute : Attribute
    {
        internal static readonly Func<object, string?> NullFactory = (obj) => null;

        private readonly object[] _objects;
        private CompletionItem[]? _items;

        public CompletionDelegate Context
        {
            get
            {
                if (_items is null) CacheItems();

                return (ctx) => _items!; //Not null as CacheItems() initializes it.
            }
        }

        /// <summary>
        /// Generates the label value, which is the text displayed to users and, unless <see cref="InsertTextFactory"/> is set, is also used to populate the <see cref="CompletionItem.InsertText"/> property.
        /// </summary>
        public Func<object, string> LabelFactory { get; init; } = (obj) => obj.ToString() ?? string.Empty;

        /// <summary>
        /// Genetares the value used to sort the completion item in a list. If this is not provided, then <see cref="LabelFactory"/> is used.
        /// </summary>
        public Func<object, string?> SortTextFactory { get; init; } = NullFactory;

        /// <summary>
        /// Generates the text to be inserted by this completion item. If this is not provided, then <see cref="LabelFactory"/> is used.
        /// </summary>
        public Func<object, string?> InsertTextFactory { get; init; } = NullFactory;

        /// <summary>
        /// Generates documentation about the completion item.
        /// </summary>
        public Func<object, string?> DocumentationFactory { get; init; } = NullFactory;

        /// <summary>
        /// Generates additional details regarding the completion item.
        /// </summary>
        public Func<object, string?> DetailFactory { get; init; } = NullFactory;

        /// <summary>
        /// Overrides all factory methods.
        /// </summary>
        public FactoryBehaviour FactoryBehaviour
        {
            init
            {
                LabelFactory = value.LabelFactory;
                SortTextFactory = value.SortTextFactory;
                InsertTextFactory = value.InsertTextFactory;
                DocumentationFactory = value.DocumentationFactory;
                DetailFactory = value.DetailFactory;
            }
        }

        public AutoCompleteAttribute(params object[] completions)
        {
            _objects = new object[completions.Length];

            for (int i = 0; i < completions.Length; i++)
            {
                var autoCompleteValue = completions[i];

                if (autoCompleteValue is null) continue;

                _objects[i] = autoCompleteValue;
            }
        }

        private void CacheItems()
        {
            _items = new CompletionItem[_objects.Length];

            for (int i = 0; i < _items.Length; i++)
            {
                var label = LabelFactory(_objects[i]);
                var sortText = SortTextFactory(_objects[i]);
                var insertText = InsertTextFactory(_objects[i]);
                var documentation = DocumentationFactory(_objects[i]);
                var detail = DetailFactory(_objects[i]);

                var completionItem = new CompletionItem(
                    label,
                    sortText: sortText,
                    insertText: insertText,
                    documentation: documentation,
                    detail: detail
                    );

                _items[i] = completionItem;
            }
        }
    }
}
