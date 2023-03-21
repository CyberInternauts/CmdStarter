namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options
{
    public class OptHandling : StarterCommand
    {
        public int MyOpt { get; set; } = 111;
        public const string MY_OPT_KEBAB = "my-opt";
    }
}
