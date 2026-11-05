using System.Reflection;
using System.CommandLine;

namespace CyberInternauts.CmdStarter.Binding;

internal sealed class ParameterBinding
{
    internal ParameterBinding(ParameterInfo parameter, ParameterBindingKind kind, Symbol? symbol)
    { Parameter = parameter; Kind = kind; Symbol = symbol; }
    internal ParameterInfo Parameter { get; }
    internal ParameterBindingKind Kind { get; }
    internal Symbol? Symbol { get; }
}
