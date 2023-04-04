﻿using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
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
        private readonly IAutoCompleteFactory? _factory;

        /// <summary>
        /// <para>
        /// If <typeparamref name="T"/> is <see langword="typeof"/> <see cref="Enum"/>
        /// generates auto completions from all values. 
        /// </para>
        /// <para>
        /// If <typeparamref name="T"/> is <see langword="typeof"/> <see cref="IAutoCompleteProvider"/>
        /// retrieves auto completions from there.
        /// </para>
        /// </summary>
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

        private static IAutoCompleteFactory GetFactory()
        {
            const string NOT_ASSIGNABLE_ERROR_MESSAGE = "{0} is not assignable from IAutoCompleteFactory.";

            if (!typeof(T).IsAssignableTo(typeof(IAutoCompleteFactory)))
            {
                var message = string.Format(NOT_ASSIGNABLE_ERROR_MESSAGE, typeof(T));
                throw new InvalidCastException(message);
            }

            var getDefaultMethod = typeof(T).GetMethod(nameof(IAutoCompleteFactory.GetDefault))!; //Cannot be null as implementation is required.

            return (IAutoCompleteFactory)getDefaultMethod.Invoke(null, null)!; //Implementation requires non-nullable return.
        }
    }
}
