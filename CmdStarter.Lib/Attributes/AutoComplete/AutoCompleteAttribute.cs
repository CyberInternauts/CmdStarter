using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
    public class AutoCompleteAttribute : Attribute
    {
        protected readonly object[] _objects;
        protected LinkedList<CompletionItem>? _items;

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
            const string NULL_ERROR_MESSAGE = "A completion cannot be null!";

            _objects = new object[completions.Length];

            for (int i = 0; i < completions.Length; i++)
            {
                var autoCompleteValue = completions[i];

                if (autoCompleteValue is null) throw new ArgumentNullException(NULL_ERROR_MESSAGE);

                _objects[i] = autoCompleteValue;
            }
        }

        protected virtual void CacheItems()
        {
            _items = new LinkedList<CompletionItem>();

            for (int i = 0; i < _objects.Length; i++)
            {
                var label = _objects[i].ToString();

                if (label is null) continue;

                var completionItem = new CompletionItem(label);

                _items.AddLast(completionItem);
            }
        }
    }
}
