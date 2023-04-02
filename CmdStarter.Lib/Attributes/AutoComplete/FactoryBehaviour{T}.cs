namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes.AutoComplete
{
    public readonly struct FactoryBehaviour<T>
    {
        /// <inheritdoc cref="AutoCompleteAttribute.LabelFactory"/>
        public required Func<T, string> LabelFactory { get; init; }

        /// <inheritdoc cref="AutoCompleteAttribute.SortTextFactory"/>
        public Func<T, string?> SortTextFactory { get; init; } = AutoCompleteAttribute<T>.NullFactory;

        /// <inheritdoc cref="AutoCompleteAttribute.InsertTextFactory"/>
        public Func<T, string?> InsertTextFactory { get; init; } = AutoCompleteAttribute<T>.NullFactory;

        /// <inheritdoc cref="AutoCompleteAttribute.DocumentationFactory"/>
        public Func<T, string?> DocumentationFactory { get; init; } = AutoCompleteAttribute<T>.NullFactory;

        /// <inheritdoc cref="AutoCompleteAttribute.DetailFactory"/>
        public Func<T, string?> DetailFactory { get; init; } = AutoCompleteAttribute<T>.NullFactory;

        public FactoryBehaviour() { }
    }
}
