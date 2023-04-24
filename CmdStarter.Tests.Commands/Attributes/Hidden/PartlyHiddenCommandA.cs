namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Hidden
{
    [Hidden]
    public sealed class PartlyHiddenCommandA : StarterCommand
    {
        public const bool COMMAND_IS_HIDDEN = true;

        public override Delegate HandlingMethod => (int parameter) => { };
        public const string NAME_OF_PARAMETER = "parameter";
        public const bool PARAMETER_IS_HIDDEN = false;

        [Hidden]
        public bool Option { get; set; }
        public const string NAME_OF_OPTION = "option";
        public const bool OPTION_IS_HIDDEN = true;
    }
}
