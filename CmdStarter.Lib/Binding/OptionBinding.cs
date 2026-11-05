using System.CommandLine;
using System.Reflection;

namespace CyberInternauts.CmdStarter.Binding;

internal sealed class OptionBinding(PropertyInfo property, Option option)
{
    internal PropertyInfo Property { get; } = property;
    internal Option Option { get; } = option;
    internal object? InitialValue { get; set; }
}
