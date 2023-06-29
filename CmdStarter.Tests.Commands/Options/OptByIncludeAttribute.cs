namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options
{
    public sealed class OptByIncludeAttribute : StarterCommand
    {
        [Option]
        public int OptionToInclude { get; set; }
        public const string OPTION_TO_INCLUDE_KEBAB = "option-to-include";

        public int OptionToExclude { get; set; }
        public const string OPTION_TO_EXCLUDE_KEBAB = "option-to-include";
    }
}
