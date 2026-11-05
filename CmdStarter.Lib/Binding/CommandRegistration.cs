using System.CommandLine;
using System.Reflection;

namespace CyberInternauts.CmdStarter.Binding;

internal sealed class CommandRegistration
{
    internal Type CommandType { get; init; } = null!;
    internal object DescriptionInstance { get; init; } = null!;
    internal Command CliCommand { get; init; } = null!;
    internal List<OptionBinding> OptionBindings { get; set; } = [];
    internal Argument?[] HandlerArguments { get; set; } = [];
    internal bool IsAvailable { get; set; } = true;

    // Set for a plain command only: the method that handles it, and how its parameters are bound. A class command is handled by its HandlingMethod.
    internal MethodInfo? PlainMethod { get; init; }
    internal IReadOnlyList<ParameterBinding> PlainParameters { get; set; } = [];
}
