namespace CyberInternauts.CmdStarter.Tests.Commands.Attributes.Description;

[Description(DESC)]
public class SingleDescByInterface : IStarterCommand
{
    public const string DESC = "One liner";

    public GlobalOptionsManager? GlobalOptionsManager { get; set; }

    public Delegate HandlingMethod => IStarterCommand.EmptyExecution;
}
