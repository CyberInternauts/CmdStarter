using CyberInternauts.CmdStarter.Tests.Common.Interfaces;

namespace CyberInternauts.CmdStarter.Tests.Commands.Options.Filters;

public sealed class OptByAttribute : StarterCommand, IOptByAttribute
{
    public static string[] IncludedOptions => [nameof(OptionToInclude)];

    public static string[] ExcludedOptions => [nameof(OptionToExclude1), nameof(OptionToExclude2)];

    [Option]
    public int OptionToInclude { get; set; }

    [Option]
    [NotOption]
    public int OptionToExclude1 { get; set; }
    public int OptionToExclude2 { get; set; }
}
