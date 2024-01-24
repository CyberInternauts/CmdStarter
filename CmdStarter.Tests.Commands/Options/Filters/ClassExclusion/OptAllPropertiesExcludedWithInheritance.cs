using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options.Filters.ClassExclusion
{
    public sealed class OptAllPropertiesExcludedWithInheritance : OptAllExcludedInheritor, IOptByAttribute
    {
        public static string[] IncludedOptions => new[] { nameof(IncludedOption) };

        public static string[] ExcludedOptions => new[] { nameof(OptionOne), nameof(OptionTwo), nameof(OptionThree), nameof(OptionFour) };

        public int IncludedOption { get; set; }
    }
}
