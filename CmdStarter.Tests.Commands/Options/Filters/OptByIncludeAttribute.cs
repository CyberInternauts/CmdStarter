using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options.Filters
{
    public sealed class OptByIncludeAttribute : StarterCommand, IOptByAttribute
    {
        public static string[] IncludedOptions => new[] { nameof(OptionToInclude) };

        public static string[] ExcludedOptions => new[] { nameof(OptionToExclude) };

        [Option]
        public int OptionToInclude { get; set; }

        public int OptionToExclude { get; set; }
    }
}
