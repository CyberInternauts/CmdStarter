namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options
{
    public sealed class OptByAttribute : StarterCommand
    {
        public int OptionToInclude { get; set; }

        public int OptionToExclude { get; set; }

        public int OptionToIgnore { get; set; }
    }
}
