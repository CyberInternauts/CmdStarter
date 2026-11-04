namespace CyberInternauts.CmdStarter.Tests.Commands.Erroneous.NameConflicts.HelpAlias;

public sealed class HostCommand : StarterCommand
{
    [Alias(true, "h")]
    public string Host { get; set; } = "";

    public override Delegate HandlingMethod => (Func<int>)(() => 0);
}
