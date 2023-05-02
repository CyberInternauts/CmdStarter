namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Hidden
{
    [Hidden]
    public sealed class HiddenCommand : StarterCommand
    {
        public const bool COMMAND_IS_HIDDEN = true;

        public override Delegate HandlingMethod => ([Hidden] int parameter) => { };
        public const string NAME_OF_PARAMETER = "parameter";
        public const bool PARAMETER_IS_HIDDEN = true;

        [Hidden]
        public bool Option { get; set; }
        public const string NAME_OF_OPTION = "option";
        public const bool OPTION_IS_HIDDEN = true;
    }
}
