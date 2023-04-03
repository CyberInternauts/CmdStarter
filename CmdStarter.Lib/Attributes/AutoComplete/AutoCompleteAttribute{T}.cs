using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes.AutoComplete;
using com.cyberinternauts.csharp.CmdStarter.Lib.Extensions;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
    public sealed class AutoCompleteAttribute<T> : AutoCompleteAttribute
    {
        internal new static readonly Func<T, string?> NullFactory = (type) => null;

        /// <inheritdoc cref="AutoCompleteAttribute.LabelFactory"/>
        public new Func<T, string> LabelFactory { get; init; } = (obj) => obj.ToString() ?? string.Empty;

        /// <inheritdoc cref="AutoCompleteAttribute.SortTextFactory"/>
        public new Func<T, string?> SortTextFactory { get; init; } = NullFactory;

        /// <inheritdoc cref="AutoCompleteAttribute.InsertTextFactory"/>
        public new Func<T, string?> InsertTextFactory { get; init; } = NullFactory;

        /// <inheritdoc cref="AutoCompleteAttribute.DocumentationFactory"/>
        public new Func<T, string?> DocumentationFactory { get; init; } = NullFactory;

        /// <inheritdoc cref="AutoCompleteAttribute.DetailFactory"/>
        public new Func<T, string?> DetailFactory { get; init; } = NullFactory;

        /// <summary>
        /// Overrides all factory methods.
        /// </summary>
        public new FactoryBehaviour<T> FactoryBehaviour
        {
            init
            {
                LabelFactory = value.LabelFactory;
                SortTextFactory = value.SortTextFactory;
                InsertTextFactory = value.InsertTextFactory;
                DocumentationFactory = value.DocumentationFactory;
                DetailFactory = value.DetailFactory;
            }
        }

        /// <summary>
        /// Creates autocompletion for all values of an <see cref="Enum"/>.
        /// </summary>
        /// <remarks>
        /// Can only be used if <typeparamref name="T"/> is <see langword="typeof"/> <see cref="Enum"/>.
        /// </remarks>
        public AutoCompleteAttribute()
            : base(HandleGenericConstructor(typeof(T)))
        { }

        public AutoCompleteAttribute(params T[] completions)
            : base(completions.ToObjectArray())
        { }

        private static object[] HandleGenericConstructor(Type type)
        {
            const string EXCEPTION_MESSAGE = "This constructor is only supported with Enums";

            if (type.IsEnum)
            {
                return Enum.GetNames(type);
            }

            throw new NotSupportedException(EXCEPTION_MESSAGE);
        }
    }
}
