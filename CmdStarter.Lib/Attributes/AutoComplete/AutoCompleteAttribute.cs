using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
    public class AutoCompleteAttribute : Attribute
    {
        const string NULL_OR_EMPTY_ERROR_MESSAGE = "A completion cannot be null or empty!";

        protected readonly string[] _labels;
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
            _labels = new string[completions.Length];

            for (int i = 0; i < completions.Length; i++)
            {
                var autoCompleteValue = completions[i]?.ToString();

                if (string.IsNullOrEmpty(autoCompleteValue)) throw new ArgumentNullException(NULL_OR_EMPTY_ERROR_MESSAGE);

                _labels[i] = autoCompleteValue;
            }
        }

        protected virtual void CacheItems()
        {
            _items = new LinkedList<CompletionItem>();

            for (int i = 0; i < _labels.Length; i++)
            {
                var label = _labels[i].ToString();

                if (label is null) continue;

                var completionItem = new CompletionItem(label);

                _items.AddLast(completionItem);
            }
        }
    }
}
