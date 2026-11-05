namespace CyberInternauts.CmdStarter.Tests.Commands.Erroneous.NameConflicts.DuplicateOptionAlias;

public sealed class TwoOptions : StarterCommand
{
    [Alias(true, "x")]
    public int First { get; set; }

    [Alias(true, "x")]
    public int Second { get; set; }

    public override Delegate HandlingMethod => (Func<int>)(() => 0);
}
