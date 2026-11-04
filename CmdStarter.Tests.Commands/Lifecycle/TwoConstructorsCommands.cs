using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;

// A service that no test registers: the long constructors below ask for it.
public sealed class MissingService
{
}

// A command with a short constructor and a long one that needs a service the container does not know.
public sealed class TwoConstructorsCommand : StarterCommand
{
    private readonly string constructor;

    public TwoConstructorsCommand() => constructor = "short";

    public TwoConstructorsCommand(MissingService service) => constructor = "long";

    public override Delegate HandlingMethod => (Action)(() => LifecycleLog.Entries.Add("command:" + constructor));
}

// The same for the class that declares a method command.
public sealed class TwoConstructorsMethods
{
    private readonly string constructor;

    public TwoConstructorsMethods() => constructor = "short";

    public TwoConstructorsMethods(MissingService service) => constructor = "long";

    [Command]
    public void Run() => LifecycleLog.Entries.Add("methods:" + constructor);
}
