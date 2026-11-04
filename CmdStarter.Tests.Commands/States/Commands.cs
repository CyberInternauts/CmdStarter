using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Tests.Commands.States;

public enum TestState
{
    Anonymous,
    Member
}

public sealed class TestStateProvider : ICommandStateProvider
{
    public object? CurrentState { get; set; }
}

[AvailableInState(TestState.Anonymous)]
public sealed class Enter : StarterCommand
{
    public override Delegate HandlingMethod => () => 10;
}

[AvailableInState(TestState.Member)]
public sealed class Profile : StarterCommand
{
    public override Delegate HandlingMethod => () => 20;
}

[Command("vault secrets")]
[AvailableInState(TestState.Member)]
public sealed class VaultSecretsCommands
{
    [Command]
    public static int List() => 60;
}

public sealed class Always : StarterCommand
{
    public override Delegate HandlingMethod => () => 30;
}

[AvailableInState(TestState.Member)]
public sealed class Account : StarterCommand
{
    public override Delegate HandlingMethod => () => 0;
}

[Parent<Account>]
public sealed class AccountSettings : StarterCommand
{
    public override Delegate HandlingMethod => () => 50;
}

public sealed class StateMethods
{
    [Command]
    [AvailableInState(TestState.Member)]
    public static int Upgrade() => 40;
}
