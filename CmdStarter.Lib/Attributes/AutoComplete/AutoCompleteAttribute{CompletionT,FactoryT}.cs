using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
    public sealed class AutoCompleteAttribute<CompletionT, FactoryT> : AutoCompleteAttribute<CompletionT>
        where FactoryT : IAutoCompleteFactory<CompletionT>
    {
        public AutoCompleteAttribute(params CompletionT[] completions)
            : base(completions)
        { }

        protected override void CacheItems()
        {
            _items = new CompletionItem[_objects.Length];

            var factory = FactoryT.GetDefault();

            for (int i = 0; i < _items.Length; i++)
            {
                var item = (CompletionT)_objects[i];

                var label = factory.LabelFactory(item);
                var sortText = factory.SortTextFactory(item);
                var insertText = factory.InsertTextFactory(item);
                var documentation = factory.DocumentationFactory(item);
                var detail = factory.DetailFactory(item);

                var completionItem = new CompletionItem(
                    label,
                    sortText: sortText,
                    insertText: insertText,
                    documentation: documentation,
                    detail: detail);

                _items[i] = completionItem;
            }
        }
    }
}
