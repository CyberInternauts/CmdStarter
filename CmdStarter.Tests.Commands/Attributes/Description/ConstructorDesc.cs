namespace CyberInternauts.CmdStarter.Tests.Commands.Attributes.Description;

public sealed class ConstructorDesc : StarterCommand
{
    public const string DESC = "From constructor";

    public ConstructorDesc() : base(nameof(ConstructorDesc), DESC) { }
}
