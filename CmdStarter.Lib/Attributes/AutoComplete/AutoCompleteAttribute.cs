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
        private const string NULL_OR_EMPTY_ERROR_MESSAGE = "A completion cannot be null or empty!";

        private readonly LinkedList<string> _labels;
        private readonly IAutoCompleteFactory? _factory;
        private LinkedList<CompletionItem>? _items;

        public CompletionDelegate Context
        {
            get
            {
                if (_items is null) CacheItems();

                return (ctx) => _items!; //Not null as CacheItems() initializes it.
            }
        }

        /// <summary>
        /// Generates completion from a given provider.
        /// <para>
        /// If <paramref name="provider"/> is <see langword="typeof"/> <see cref="Enum"/>
        /// generates auto completions from all values. 
        /// </para>
        /// <para>
        /// If <paramref name="provider"/> is <see langword="typeof"/> <see cref="IAutoCompleteProvider"/>
        /// retrieves auto completions from there.
        /// </para>
        /// </summary>
        /// <param name="provider"><see cref="Type"/> of an <see cref="Enum"/> or an <see cref="IAutoCompleteProvider"/>.</param>
        public AutoCompleteAttribute(Type provider)
            : this(provider, Array.Empty<string>())
        { }

        /// <summary>
        /// Creates auto completions from the given parameters.
        /// </summary>
        /// <param name="completions">Labels for the auto completions.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public AutoCompleteAttribute(params object[] completions)
        {
            _labels = new LinkedList<string>();

            for (int i = 0; i < completions.Length; i++)
            {
                var autoCompleteValue = completions[i]?.ToString();

                if (string.IsNullOrWhiteSpace(autoCompleteValue))
                {
                    var paramName = $"{nameof(completions)}[{i}]";
                    throw new ArgumentNullException(paramName, NULL_OR_EMPTY_ERROR_MESSAGE);
                }

                _labels.AddLast(autoCompleteValue);
            }
        }

        /// <summary>
        /// Creates auto completions from the given <paramref name="completions"/> and runs them through the <paramref name="provider"/>.
        /// </summary>
        /// <param name="provider">Must be <see langword="typeof"/> <see cref="IAutoCompleteFactory"/>.</param>
        /// <param name="completions">Labels for the auto completions.</param>
        public AutoCompleteAttribute(Type provider, params object[] completions)
            : this(completions)
        {
            const string EXCEPTION_MESSAGE = "Provider must be an Enum or inherit IAutoCompleteProvider.";

            bool hadCast = false;

            if (provider.IsEnum)
            {
                var enumNames = Enum.GetNames(provider);
                foreach (var item in enumNames)
                {
                    _labels.AddLast(item);
                }

                hadCast = true;
            }
            else if (provider.IsAssignableTo(typeof(IAutoCompleteProvider)))
            {
                var getDefaultMethod = provider.GetMethod(nameof(IAutoCompleteProvider.GetInstance))!; //Cannot be null as implementation is required.

                var instance = (IAutoCompleteProvider)getDefaultMethod.Invoke(null, null)!; //Implementation requires non-nullable return.

                var autoCompletes = instance.GetAutoCompletes();
                for (int i = 0; i < autoCompletes.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(autoCompletes[i])) continue;

                    _labels.AddLast(autoCompletes[i]);
                }

                hadCast = true;
            }

            if (!hadCast && completions.Length == 0) throw new NotSupportedException(EXCEPTION_MESSAGE);

            if (provider.IsAssignableTo(typeof(IAutoCompleteFactory)))
            {
                var getDefaultMethod = provider.GetMethod(nameof(IAutoCompleteFactory.GetInstance))!; //Cannot be null as implementation is required.

                _factory = (IAutoCompleteFactory)getDefaultMethod.Invoke(null, null)!; //Implementation requires non-nullable return.
            }
        }

        private void CacheItems()
        {
            _items = new LinkedList<CompletionItem>();

            foreach (var label in _labels)
            {
                if (string.IsNullOrWhiteSpace(label)) continue;

                var sortText = _factory?.GetSortText(label);
                var insertText = _factory?.GetInsertText(label);
                var documentation = _factory?.GetDocumentation(label);
                var detail = _factory?.GetDetail(label);

                var completionItem = new CompletionItem(
                    label,
                    sortText: sortText,
                    insertText: insertText,
                    documentation: documentation,
                    detail: detail);

                _items.AddLast(completionItem);
            }
        }
    }
}
