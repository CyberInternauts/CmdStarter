using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
    public class AutoCompleteAttribute : Attribute
    {
        private readonly CompletionItem[] _items;

        public CompletionDelegate Context => (CompletionContext ctx) => _items;

        public AutoCompleteAttribute(params object[] completions)
        {
            _items = new CompletionItem[completions.Length];

            for (int i = 0; i < completions.Length; i++)
            {
                var autoCompleteValue = completions[i];

                if (autoCompleteValue is null) continue;

                CompletionItem completionItem = new(label: autoCompleteValue.ToString() ?? string.Empty);
                _items[i] = completionItem;
            }
        }
    }
}
