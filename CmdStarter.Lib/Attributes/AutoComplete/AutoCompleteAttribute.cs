using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
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

        public AutoCompleteAttribute(Type type)
            : this(HandleType(type))
        { }
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

        protected static object[] HandleType(Type type)
        {
            const string EXCEPTION_MESSAGE = "This constructor is only supported with Enums and IAutoCompleteProvider.";

            if (type.IsEnum) return Enum.GetNames(type);

            if (type.IsAssignableFrom(typeof(IAutoCompleteProvider)))
            {
                var getDefaultMethod = type.GetMethod(nameof(IAutoCompleteProvider.GetDefault))!; //Cannot be null as implementation is required.

                var instance = (IAutoCompleteProvider)getDefaultMethod.Invoke(null, null)!; //Implementation requires non-nullable return.

                var autoCompletes = instance.GetAutoCompletes();
                var hasNullItem = autoCompletes.Any(autoComplete => string.IsNullOrEmpty(autoComplete));

                if (hasNullItem) throw new ArgumentNullException(NULL_OR_EMPTY_ERROR_MESSAGE);

                return autoCompletes;
            }

            throw new NotSupportedException(EXCEPTION_MESSAGE);
        }
    }
}
