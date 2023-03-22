namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options
{
    public class OptCompleteDerived : OptComplete<OptCompleteDerived>
    {
        public int NewIntOpt { get; set; }
        public const string NEW_INT_OPT_KEBAB = "new-int-opt";
    }
}
