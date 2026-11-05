namespace CyberInternauts.CmdStarter.Tests.Commands.NamespaceGroups;

public class Status : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 41);
}

public class About : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 42);
}

public class ToolGroup : StarterCommand
{
    public ToolGroup() : base("tools")
    {
    }
}

public class Host : StarterCommand
{
}
