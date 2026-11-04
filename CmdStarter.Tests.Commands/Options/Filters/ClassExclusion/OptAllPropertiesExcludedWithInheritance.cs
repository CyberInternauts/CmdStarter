using CyberInternauts.CmdStarter.Tests.Common.Interfaces;

namespace CyberInternauts.CmdStarter.Tests.Commands.Options.Filters.ClassExclusion;

public sealed class OptAllPropertiesExcludedWithInheritance : OptAllExcludedInheritor, IOptByAttribute
{
    public static string[] IncludedOptions => [nameof(IncludedOption)];

    public static string[] ExcludedOptions => [nameof(OptionOne), nameof(OptionTwo), nameof(OptionThree), nameof(OptionFour)];

    public int IncludedOption { get; set; }
}
