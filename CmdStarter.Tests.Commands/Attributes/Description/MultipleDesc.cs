namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Description
{
    [Description(FIRST_DESC)]
    [MoreDescription(SECOND_DESC)]
    public class MultipleDesc : StarterCommand
    {
        public const string FIRST_DESC = "First line";
        public const string SECOND_DESC = "Second line";
    }
}
