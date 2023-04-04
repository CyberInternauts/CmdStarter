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
            _items = new LinkedList<CompletionItem>();

            var factory = FactoryT.GetDefault();

            for (int i = 0; i < _objects.Length; i++)
            {
                var item = (CompletionT)_objects[i];

                var label = factory.GetLabel(item);
                var sortText = factory.GetSortText(item);
                var insertText = factory.GetInsertText(item);
                var documentation = factory.GetDocumentation(item);
                var detail = factory.GetDetail(item);

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
