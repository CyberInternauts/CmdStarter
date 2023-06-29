using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options.Attributes
{
    public sealed class OptByExcludeAttribute : StarterCommand, IOptByAttribute
    {
        public static string[] IncludedOptions => new[] { nameof(OptionToInclude) };

        public static string[] ExcludedOptions => new[] { nameof(OptionToExclude) };

        public int OptionToInclude { get; set; }

        [NotOption]
        public int OptionToExclude { get; set; }
    }
}
