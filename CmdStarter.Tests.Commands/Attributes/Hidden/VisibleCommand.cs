namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Hidden
{

    public sealed class VisibleCommand : StarterCommand
    {
        public override Delegate MethodForHandling => (int visibleParameter) => { };
        public const string NAME_OF_VISIBLE_PARAMETER = "visibleParameter";

        public bool VisibleOption { get; set; }
        public const string NAME_OF_VISIBLE_OPTION = "visible-option";
    }
}
