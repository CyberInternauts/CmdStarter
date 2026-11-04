namespace CyberInternauts.CmdStarter.Tests.Commands.Erroneous.NameConflicts.AliasVsName;

public sealed class Alpha : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 0);
}

[Alias("alpha")]
public sealed class Beta : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 0);
}
