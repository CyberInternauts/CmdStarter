using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter.Tests.Commands.FluentTree;

namespace CyberInternauts.CmdStarter.Tests.Commands.FluentTree;

public class Users : StarterCommand
{
}

public class Groups : StarterCommand
{
}

public class ListUsers : StarterCommand
{
    public ListUsers() : base("list")
    {
    }

    public override Delegate HandlingMethod => (Func<int>)(() => 31);
}

public class ListGroups : StarterCommand
{
    public ListGroups() : base("list")
    {
    }

    public override Delegate HandlingMethod => (Func<int>)(() => 32);
}

public class Status : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 33);
}

[Parent<Groups>]
public class Orphan : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 34);
}

public class InterfaceLeaf : IStarterCommand
{
    public GlobalOptionsManager? GlobalOptionsManager { get; set; }

    public Delegate HandlingMethod => (Func<int>)(() => 36);
}
