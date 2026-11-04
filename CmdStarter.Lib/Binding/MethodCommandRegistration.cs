using System.CommandLine;
using System.Reflection;

namespace CyberInternauts.CmdStarter.Binding;

internal sealed class MethodCommandRegistration
{
    internal MethodCommandRegistration(MethodInfo method, Command cliCommand, IReadOnlyList<ParameterBinding> parameters)
    { Method = method; CliCommand = cliCommand; Parameters = parameters; }
    internal MethodInfo Method { get; }
    internal Command CliCommand { get; }
    internal IReadOnlyList<ParameterBinding> Parameters { get; }
    internal bool IsAvailable { get; set; } = true;
}
