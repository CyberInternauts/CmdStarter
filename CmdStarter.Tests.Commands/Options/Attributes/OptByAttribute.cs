namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options.Attributes
{
        public static string[] ExcludedOptions => new[] { nameof(OptionToExclude1), nameof(OptionToExclude2) };

        [Option]
        public int OptionToInclude { get; set; }

        [Option]
        [NotOption]
        public int OptionToExclude1 { get; set; }
        public int OptionToExclude2 { get; set; }
    }
}
