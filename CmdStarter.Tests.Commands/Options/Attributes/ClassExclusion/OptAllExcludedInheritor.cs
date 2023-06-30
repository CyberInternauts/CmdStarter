namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options.Attributes.ClassExclusion
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