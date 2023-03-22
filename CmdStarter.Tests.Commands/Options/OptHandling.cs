namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options
{
    public class OptHandling : StarterCommand<OptHandling>
    {
        public int MyOptInt { get; set; } = 111;
        public const string MY_OPT_INT_KEBAB = "my-opt-int";

        public bool MyOptBool { get; set; } = false;
        public const string MY_OPT_BOOL_KEBAB = "my-opt-bool";
    }
}
