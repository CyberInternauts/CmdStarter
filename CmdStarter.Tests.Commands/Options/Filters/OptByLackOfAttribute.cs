using CyberInternauts.CmdStarter.Tests.Common.Interfaces;

namespace CyberInternauts.CmdStarter.Tests.Commands.Options.Filters;

public sealed class OptByLackOfAttribute : StarterCommand, IOptByAttribute
{
    public static string[] IncludedOptions => [nameof(Option1), nameof(Option2)];

    public static string[] ExcludedOptions => Array.Empty<string>();

    public int Option1 { get; set; }
    public int Option2 { get; set; }
}
