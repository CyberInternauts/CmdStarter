using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces
{
    public interface IHasAutoComplete
    {
        IEnumerable<CompletionItem> OptionCompletionsExpected();

        IEnumerable<CompletionItem> ArgumentCompletionsExpected();
    }
}
