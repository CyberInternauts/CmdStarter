using System.CommandLine.Completions;

namespace CyberInternauts.CmdStarter.Tests.Common.Interfaces;

public interface IHasAutoComplete
{
    IEnumerable<CompletionItem> OptionCompletionsExpected();

    IEnumerable<CompletionItem> ArgumentCompletionsExpected();
}
