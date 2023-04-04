using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
    public sealed class AutoCompleteAttribute<T> : AutoCompleteAttribute
    {
        private readonly IAutoCompleteFactory? _factory;

        /// <summary>
        /// Creates autocompletion for all values of an <see cref="Enum"/>.
        /// </summary>
        /// <remarks>
        /// Can only be used if <typeparamref name="T"/> is <see langword="typeof"/> <see cref="Enum"/>.
        /// </remarks>
        public AutoCompleteAttribute()
            : base(typeof(T))
        { }

        /// <inheritdoc cref="AutoCompleteAttribute(object[])"/>
        public AutoCompleteAttribute(params object[] completions)
            : base(completions)
        {
            _factory = GetFactory();
        }

        /// <inheritdoc cref="AutoCompleteAttribute(Type)"/>
        public AutoCompleteAttribute(Type type)
            : base(type)
        {
            _factory = GetFactory();
        }

        protected sealed override void CacheItems()
        {
            if (_factory is null)
            {
                base.CacheItems();
                return;
            }

            _items = new LinkedList<CompletionItem>();

            for (int i = 0; i < _labels.Length; i++)
            {
                var label = _labels[i];

                var sortText = _factory.GetSortText(label);
                var insertText = _factory.GetInsertText(label);
                var documentation = _factory.GetDocumentation(label);
                var detail = _factory.GetDetail(label);

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
}
