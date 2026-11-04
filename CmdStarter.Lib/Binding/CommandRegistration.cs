using System.CommandLine;
using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Binding;

internal sealed class CommandRegistration
{
    internal Type CommandType { get; init; } = null!;
    internal IStarterCommand DescriptionInstance { get; init; } = null!;
    internal Command CliCommand { get; init; } = null!;
    internal List<OptionBinding> OptionBindings { get; set; } = new();
    internal Argument?[] HandlerArguments { get; set; } = Array.Empty<Argument?>();
    internal bool IsAvailable { get; set; } = true;
}
