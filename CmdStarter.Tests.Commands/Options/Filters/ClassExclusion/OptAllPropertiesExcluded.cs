using CyberInternauts.CmdStarter.Tests.Common.Interfaces;

namespace CyberInternauts.CmdStarter.Tests.Commands.Options.Filters.ClassExclusion;

[AllOptionsExcluded]
public class OptAllPropertiesExcluded : StarterCommand, IOptByAttribute
{
    public static string[] IncludedOptions => Array.Empty<string>();

    public static string[] ExcludedOptions => new[] { nameof(OptionOne), nameof(OptionTwo), nameof(OptionThree), nameof(OptionFour) };

    public int OptionOne { get; set; }
    public int OptionTwo { get; set; }
    public int OptionThree { get; set; }
    public int OptionFour { get; set; }
}