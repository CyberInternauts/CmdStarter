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

        /// <summary>
        /// Return a completion delegate that provides the items of possible completions.
        /// </summary>
        public CompletionDelegate Context
        {
            get
            {
                if (_items is null) CacheItems();

                return (ctx) => _items!; //Not null as CacheItems() initializes it.
            }
        }

        /// <summary>
        /// Generates completion from a given features type.
        /// <para>
        /// If <paramref name="features"/> is <see langword="typeof"/> <see cref="Enum"/>
        /// generates auto completions from all values. 
        /// </para>
        /// <para>
        /// If <paramref name="features"/> is <see langword="typeof"/> <see cref="IAutoCompleteProvider"/>
        /// retrieves auto completions from there.
        /// </para>
        /// <para>
        /// If <paramref name="features"/> is <see langword="typeof"/> <see cref="IAutoCompleteFactory"/>
        /// provides other completion information.
        /// </para>
        /// </summary>
        /// <param name="features"><see cref="Type"/> of an <see cref="Enum"/> or an <see cref="IAutoCompleteProvider"/>.</param>
        public AutoCompleteAttribute(Type features)
            : this(features, Array.Empty<string>())
        { }

        /// <summary>
        /// Creates auto completions from the given parameters.
        /// </summary>
        /// <param name="completions">Labels for the auto completions.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public AutoCompleteAttribute(params object[] completions)
        {
            _labels = new LinkedList<string>();
            FillLabels(completions);

            if (_labels.Count == 0)
            {
                throw new NotSupportedException("Shall have at least one completion");
            }
        }

        /// <summary>
        /// Creates auto completions from the given <paramref name="completions"/> and runs them through the <paramref name="features"/>.
        /// </summary>
        /// <param name="features">Must be <see langword="typeof"/> <see cref="IAutoCompleteFactory"/>.</param>
        /// <param name="completions">Labels for the auto completions.</param>
        public AutoCompleteAttribute(Type features, params object[] completions)
        {
            _labels = new LinkedList<string>();
            FillLabels(completions);

            const string EXCEPTION_MESSAGE = "The parameter " + nameof(features) + " must be an Enum or implement "
                + nameof(IAutoCompleteProvider) + " or " + nameof(IAutoCompleteFactory)
                + " and having at least one completion";

            bool isSupported = false;

            if (features.IsEnum)
            {
                var enumNames = Enum.GetNames(features);
                FillLabels(enumNames);

                isSupported = true;
            }
            else if (features.IsAssignableTo(typeof(IAutoCompleteProvider)))
            {
                var getDefaultMethod = features.GetMethod(nameof(IAutoCompleteProvider.GetInstance))!; //Cannot be null as implementation is required.
                var instance = (IAutoCompleteProvider)getDefaultMethod.Invoke(null, null)!; //Implementation requires non-nullable return.

                var autoCompletes = instance.GetAutoCompletes();
                FillLabels(autoCompletes);

                isSupported = true;
            }

            if (features.IsAssignableTo(typeof(IAutoCompleteFactory)))
            {
                var getDefaultMethod = features.GetMethod(nameof(IAutoCompleteFactory.GetInstance))!; //Cannot be null as implementation is required.

                _factory = (IAutoCompleteFactory)getDefaultMethod.Invoke(null, null)!; //Implementation requires non-nullable return.

                isSupported = true;
            }

            if (!isSupported || _labels.Count == 0)
            {
                throw new NotSupportedException(EXCEPTION_MESSAGE);
            }
        }

        private void FillLabels(object[] completions)
        {
            for (int i = 0; i < completions.Length; i++)
            {
                var autoCompleteValue = completions[i]?.ToString();
                if (string.IsNullOrWhiteSpace(autoCompleteValue)) continue;

                _labels.AddLast(autoCompleteValue);
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


    /// <inheritdoc cref="AutoCompleteAttribute"/>
    /// <typeparam name="T">
    /// Defines the behaviour of the attribute.
    /// <para>
    /// More info in constructors.
    /// </para>
    /// </typeparam>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
    public sealed class AutoCompleteAttribute<T> : AutoCompleteAttribute
    {
        /// <summary>
        /// <para>
        /// If <typeparamref name="T"/> is <see langword="typeof"/> <see cref="Enum"/>
        /// generates auto completions from all values. 
        /// </para>
        /// <para>
        /// If <typeparamref name="T"/> is <see langword="typeof"/> <see cref="IAutoCompleteProvider"/>
        /// retrieves auto completions from there.
        /// </para>
        /// <para>
        /// Can also be <see langword="typeof"/> <see cref="IAutoCompleteFactory"/> besides these.
        /// </para>
        /// </summary>
        public AutoCompleteAttribute()
            : base(typeof(T))
        { }

        /// <summary>
        /// Creates auto completions from the given <paramref name="completions"/> and handles <typeparamref name="T"/>.
        /// <para>
        /// <typeparamref name="T"/> must be <see langword="typeof"/> <see cref="IAutoCompleteProvider"/>.
        /// <para>
        /// Can also be <see langword="typeof"/> <see cref="IAutoCompleteFactory"/>.
        /// </para>
        /// </para>
        /// </summary>
        /// <param name="completions">Labels for the auto completions.</param>
        public AutoCompleteAttribute(params object[] completions)
            : base(typeof(T), completions)
        { }
    }
}
