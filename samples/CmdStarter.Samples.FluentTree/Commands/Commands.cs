namespace CyberInternauts.CmdStarter.Samples.FluentTree.Commands;

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

    public override Delegate HandlingMethod => (Action)(() => Console.Out.WriteLine("ann, bob"));
}

public class ListGroups : StarterCommand
{
    public ListGroups() : base("list")
    {
    }

    public override Delegate HandlingMethod => (Action)(() => Console.Out.WriteLine("admins, devs"));
}

public class Reset : StarterCommand
{
    public override Delegate HandlingMethod => (Action)(() => Console.Out.WriteLine("reset done"));
}
