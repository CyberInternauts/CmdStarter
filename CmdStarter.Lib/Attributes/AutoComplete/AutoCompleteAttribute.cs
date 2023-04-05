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

        private readonly string[] _labels;
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
            : this(HandleProviderType(provider))
        { }

        /// <summary>
        /// Generates completion from a given provider and runs them through the <paramref name="factory"/>.
        /// <para>
        /// If <paramref name="provider"/> is <see langword="typeof"/> <see cref="Enum"/>
        /// generates auto completions from all values. 
        /// </para>
        /// <para>
        /// If <paramref name="provider"/> is <see langword="typeof"/> <see cref="IAutoCompleteProvider"/>
        /// retrieves auto completions from there.
        /// </para>
        /// </summary>
        /// <param name="factory">Must be <see langword="typeof"/> <see cref="IAutoCompleteFactory"/>.</param>
        /// <param name="provider"><see cref="Type"/> of an <see cref="Enum"/> or an <see cref="IAutoCompleteProvider"/>.</param>
        public AutoCompleteAttribute(Type factory, Type provider)
            : this(factory, HandleProviderType(provider))
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

        /// <summary>
        /// Creates auto completions from the given <paramref name="completions"/> and runs them through the <paramref name="factory"/>.
        /// </summary>
        /// <param name="factory">Must be <see langword="typeof"/> <see cref="IAutoCompleteFactory"/>.</param>
        /// <param name="completions">Labels for the auto completions.</param>
        public AutoCompleteAttribute(Type factory, params object[] completions)
            : this(completions)
        {
            _factory = GetFactory(factory);
        }

        private void CacheItems()
        {
            _items = new LinkedList<CompletionItem>();

            for (int i = 0; i < _labels.Length; i++)
            {
                var label = _labels[i];

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

        private static object[] HandleProviderType(Type type)
        {
            const string EXCEPTION_MESSAGE = "This constructor is only supported with Enums and IAutoCompleteProvider.";

            if (type.IsEnum) return Enum.GetNames(type);

            if (type.IsAssignableTo(typeof(IAutoCompleteProvider)))
            {
                var getDefaultMethod = type.GetMethod(nameof(IAutoCompleteProvider.GetInstance))!; //Cannot be null as implementation is required.

                var instance = (IAutoCompleteProvider)getDefaultMethod.Invoke(null, null)!; //Implementation requires non-nullable return.

                var autoCompletes = instance.GetAutoCompletes();
                for (int i = 0; i < autoCompletes.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(autoCompletes[i])) continue;

                    var paramName = $"{nameof(autoCompletes)}[{i}]";
                    throw new ArgumentNullException(paramName, NULL_OR_EMPTY_ERROR_MESSAGE);
                }

                return autoCompletes;
            }

            throw new NotSupportedException(EXCEPTION_MESSAGE);
        }

        private static IAutoCompleteFactory GetFactory(Type type)
        {
            const string NOT_ASSIGNABLE_ERROR_MESSAGE = "{0} is not assignable from IAutoCompleteFactory.";

            if (!type.IsAssignableTo(typeof(IAutoCompleteFactory)))
            {
                var message = string.Format(NOT_ASSIGNABLE_ERROR_MESSAGE, type);
                throw new InvalidCastException(message);
            }

            var getDefaultMethod = type.GetMethod(nameof(IAutoCompleteFactory.GetInstance))!; //Cannot be null as implementation is required.

            return (IAutoCompleteFactory)getDefaultMethod.Invoke(null, null)!; //Implementation requires non-nullable return.
        }
    }
}
