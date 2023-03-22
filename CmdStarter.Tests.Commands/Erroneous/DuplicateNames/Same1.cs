namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.DuplicateNames
{
    public class Same1 : StarterCommand<Same1>
    {
        public Same1() : base("Same") { }
    }
}
