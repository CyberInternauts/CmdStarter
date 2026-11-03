using System.CommandLine;
using System.Reflection;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Binding;

internal sealed class OptionBinding(PropertyInfo property, Option option)
{
    internal PropertyInfo Property { get; } = property;
    internal Option Option { get; } = option;
    internal object? InitialValue { get; set; }
}
