namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes.AutoComplete
{
    public readonly struct FactoryBehaviour
    {
        /// <inheritdoc cref="AutoCompleteAttribute.LabelFactory"/>
        public required Func<object, string> LabelFactory { get; init; }

        /// <inheritdoc cref="AutoCompleteAttribute.SortTextFactory"/>
        public Func<object, string?> SortTextFactory { get; init; } = AutoCompleteAttribute.NullFactory;

        /// <inheritdoc cref="AutoCompleteAttribute.InsertTextFactory"/>
        public Func<object, string?> InsertTextFactory { get; init; } = AutoCompleteAttribute.NullFactory;

        /// <inheritdoc cref="AutoCompleteAttribute.DocumentationFactory"/>
        public Func<object, string?> DocumentationFactory { get; init; } = AutoCompleteAttribute.NullFactory;

        /// <inheritdoc cref="AutoCompleteAttribute.DetailFactory"/>
        public Func<object, string?> DetailFactory { get; init; } = AutoCompleteAttribute.NullFactory;

        public FactoryBehaviour() { }
    }
}
