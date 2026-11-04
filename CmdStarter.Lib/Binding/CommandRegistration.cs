using System.CommandLine;
using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Binding;

internal sealed class CommandRegistration
{
    internal Type CommandType { get; init; } = null!;
    internal IStarterCommand DescriptionInstance { get; init; } = null!;
    internal Command CliCommand { get; init; } = null!;
    internal List<OptionBinding> OptionBindings { get; set; } = [];
    internal Argument?[] HandlerArguments { get; set; } = [];
    internal bool IsAvailable { get; set; } = true;
}
