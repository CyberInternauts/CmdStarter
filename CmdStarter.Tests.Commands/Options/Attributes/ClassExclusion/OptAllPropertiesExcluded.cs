namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options.Attributes.ClassExclusion
{
    [AllOptionsExcluded]
    public class OptAllPropertiesExcluded : StarterCommand
    {
        public const int OPTION_COUNT = 0;

        public int OptionOne { get; set; }
        public int OptionTwo { get; set; }
        public int OptionThree { get; set; }
        public int OptionFour { get; set; }
    }
}
