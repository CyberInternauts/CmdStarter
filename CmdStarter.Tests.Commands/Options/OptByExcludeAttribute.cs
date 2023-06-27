namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options
{
    public sealed class OptByExcludeAttribute : StarterCommand
    {
        public int OptionToInclude { get; set; }

        [NotOption]
        public int OptionToExclude { get; set; }
    }
}
