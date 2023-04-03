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
