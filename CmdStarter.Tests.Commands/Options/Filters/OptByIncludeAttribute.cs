using CyberInternauts.CmdStarter.Tests.Common.Interfaces;

namespace CyberInternauts.CmdStarter.Tests.Commands.Options.Filters;

public sealed class OptByIncludeAttribute : StarterCommand, IOptByAttribute
{
    public static string[] IncludedOptions => [nameof(OptionToInclude)];

    public static string[] ExcludedOptions => [nameof(OptionToExclude)];

    [Option]
    public int OptionToInclude { get; set; }

    public int OptionToExclude { get; set; }
}
