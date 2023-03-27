namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Hidden
{
    [Hidden]
    public sealed class HiddenCommand : StarterCommand
    {
        public override Delegate MethodForHandling => ([Hidden] int hiddenParameter) => { };
        public const string NAME_OF_HIDDEN_PARAMETER = "hiddenParameter";

        [Hidden]
        public bool HiddenOption { get; set; }
        public const string NAME_OF_HIDDEN_OPTION = "hidden-option";
    }
}
