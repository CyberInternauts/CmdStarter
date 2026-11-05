using CyberInternauts.CmdStarter.Tests.Common.Interfaces;

namespace CyberInternauts.CmdStarter.Tests.Commands.Options.Filters;

public sealed class OptByExcludeAttribute : StarterCommand, IOptByAttribute
{
    public static string[] IncludedOptions => [nameof(OptionToInclude)];

    public static string[] ExcludedOptions => [nameof(OptionToExclude)];

    public int OptionToInclude { get; set; }

    [NotOption]
    public int OptionToExclude { get; set; }
}
