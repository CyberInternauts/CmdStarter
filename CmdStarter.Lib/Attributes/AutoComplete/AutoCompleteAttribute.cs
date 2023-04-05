using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
    /// <summary>
    /// Defines auto-completions for an <see cref="System.CommandLine.Option"/> or an <see cref="System.CommandLine.Argument"/>.
    /// </summary>
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

        /// <summary>
        /// <para>
        /// If <paramref name="type"/> is <see langword="typeof"/> <see cref="Enum"/>
        /// generates auto completions from all values. 
        /// </para>
        /// <para>
        /// If <paramref name="type"/> is <see langword="typeof"/> <see cref="IAutoCompleteProvider"/>
        /// retrieves auto completions from there.
        /// </para>
        /// </summary>
        /// <param name="type">A <see cref="Type"/> of an <see cref="Enum"/> or an <see cref="IAutoCompleteProvider"/>.</param>
        public AutoCompleteAttribute(Type type)
            : this(HandleType(type))
        { }

        /// <summary>
        /// Creates auto completions from the given parameters.
        /// </summary>
        /// <param name="completions">Labels for the auto completions.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public AutoCompleteAttribute(params object[] completions)
        {
            _labels = new string[completions.Length];

            for (int i = 0; i < completions.Length; i++)
            {
                var autoCompleteValue = completions[i]?.ToString();

                if (string.IsNullOrWhiteSpace(autoCompleteValue))
                {
                    var paramName = $"{nameof(completions)}[{i}]";
                    throw new ArgumentNullException(paramName, NULL_OR_EMPTY_ERROR_MESSAGE);
                }


                _labels[i] = autoCompleteValue;
            }
        }

        protected virtual void CacheItems()
        {
            _items = new LinkedList<CompletionItem>();

            for (int i = 0; i < _labels.Length; i++)
            {
                var label = _labels[i];

                if (string.IsNullOrWhiteSpace(label)) continue;

                var completionItem = new CompletionItem(label);

                _items.AddLast(completionItem);
            }
        }

        protected static object[] HandleType(Type type)
        {
            const string EXCEPTION_MESSAGE = "This constructor is only supported with Enums and IAutoCompleteProvider.";

            if (type.IsEnum) return Enum.GetNames(type);

            if (type.IsAssignableTo(typeof(IAutoCompleteProvider)))
            {
                var getDefaultMethod = type.GetMethod(nameof(IAutoCompleteProvider.GetDefault))!; //Cannot be null as implementation is required.

                var instance = (IAutoCompleteProvider)getDefaultMethod.Invoke(null, null)!; //Implementation requires non-nullable return.

                var autoCompletes = instance.GetAutoCompletes();
                for (int i = 0; i < autoCompletes.Length; i++)
                {
                    var paramName = $"{nameof(autoCompletes)}[{i}]";
                    throw new ArgumentNullException(paramName, NULL_OR_EMPTY_ERROR_MESSAGE);
                }

                return autoCompletes;
            }

            throw new NotSupportedException(EXCEPTION_MESSAGE);
        }
    }
}
