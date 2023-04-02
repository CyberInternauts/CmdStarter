using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
    public class AutoCompleteAttribute : Attribute
    {
        private readonly object[] _objects;
        private CompletionItem[]? _items;

        public CompletionDelegate Context
        {
            get
            {
                if (_items is null) throw new NotImplementedException();

                return (ctx) => _items;
            }
        }

        /// <summary>
        /// Generates the label value, which is the text displayed to users and, unless <see cref="InsertTextFactory"/> is set, is also used to populate the <see cref="CompletionItem.InsertText"/> property.
        /// </summary>
        public Func<object, string> LabelFactory { get; init; } = (obj) => obj.ToString() ?? string.Empty;

        /// <summary>
        /// Genetares the value used to sort the completion item in a list. If this is not provided, then <see cref="LabelFactory"/> is used.
        /// </summary>
        public Func<object, string>? SortTextFactory { get; init; }

        /// <summary>
        /// Generates the text to be inserted by this completion item. If this is not provided, then <see cref="LabelFactory"/> is used.
        /// </summary>
        public Func<object, string>? InsertTextFactory { get; init; }

        /// <summary>
        /// Generates documentation about the completion item.
        /// </summary>
        public Func<object, string>? DocumentationFactory { get; init; }

        /// <summary>
        /// Generates additional details regarding the completion item.
        /// </summary>
        public Func<object, string>? DetailFactory { get; init; }

        public AutoCompleteAttribute(params object[] completions)
        {
            _objects = new object[completions.Length];

            for (int i = 0; i < completions.Length; i++)
            {
                var autoCompleteValue = completions[i];

                if (autoCompleteValue is null) continue;

                _objects[i] = autoCompleteValue;
            }
        }
    }
}
