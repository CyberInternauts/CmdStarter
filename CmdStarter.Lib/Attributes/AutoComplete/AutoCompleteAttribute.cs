using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
    public class AutoCompleteAttribute : Attribute
    {
        protected readonly object[] _objects;
        protected CompletionItem[]? _items;

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

                if (autoCompleteValue is null) throw new ArgumentNullException(autoCompleteValue?.ToString());

                _objects[i] = autoCompleteValue;
            }
        }

        protected virtual void CacheItems()
        {
            _items = new CompletionItem[_objects.Length];

            for (int i = 0; i < _items.Length; i++)
            {
                var label = _objects[i].ToString() ?? string.Empty;
                var completionItem = new CompletionItem(label);

                _items[i] = completionItem;
            }
        }
    }
}
