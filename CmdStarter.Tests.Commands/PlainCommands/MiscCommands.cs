using CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;
using CyberInternauts.CmdStarter.Tests.Commands.States;

namespace CyberInternauts.CmdStarter.Tests.Commands.PlainCommands;

[Command]
[AvailableInState(TestState.Member)]
public sealed class MemberPlain
{
    public int Run() => 51;
}

[Command]
[Hidden]
[Alias("dec", "decorated")]
[Description("A decorated plain command")]
public sealed class DecoratedPlain
{
    public int Run() => 61;
}

// No parameterless constructor: only a factory, or dependency injection, can create it.
[Command]
public sealed class FactoryPlain(string label)
{
    public int Run()
    {
        PlainLog.Entries.Add(label);
        return 71;
    }
}

[Command]
public sealed class InjectedPlain(SingletonService singleton, ScopedService scoped)
{
    public int Run()
    {
        PlainLog.Received.Add(singleton);
        PlainLog.Received.Add(scoped);
        return 72;
    }
}
