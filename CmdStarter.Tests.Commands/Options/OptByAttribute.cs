namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options
{
    public sealed class OptByAttribute : StarterCommand
    {
        [Option]
        public int OptionToInclude { get; set; }
        public const string OPTION_TO_INCLUDE_KEBAB = "option-to-include";

        [Option]
        [NotOption]
        public int OptionToExclude1 { get; set; }

        public int OptionToExclude2 { get; set; }

        public const string OPTION_TO_EXCLUDE_KEBAB = "option-to-include";
    }
}
