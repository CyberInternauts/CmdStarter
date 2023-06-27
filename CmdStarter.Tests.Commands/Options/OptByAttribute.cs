namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options
{
    public sealed class OptByAttribute : StarterCommand
    {
        [Option]
        public int OptionToInclude { get; set; }

        [Option]
        [NotOption]
        public int OptionToExclude { get; set; }

        public int OptionToIgnore { get; set; }
    }
}
