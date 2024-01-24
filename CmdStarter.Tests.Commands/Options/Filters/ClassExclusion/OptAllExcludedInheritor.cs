namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options.Filters.ClassExclusion
{
    [AllOptionsExcluded]
    public class OptAllExcludedInheritor : StarterCommand
    {
        public int OptionOne { get; set; }
        public int OptionTwo { get; set; }
        public int OptionThree { get; set; }
        public int OptionFour { get; set; }
    }
}