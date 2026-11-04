using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Tests.Commands.Remote;

public sealed class Hello : StarterCommand
{
    public override Delegate HandlingMethod => (Func<CommandConsole, int>)(console =>
    {
        console.Out.WriteLine("hello");
        return 0;
    });
}

public sealed class Oops : StarterCommand
{
    public override Delegate HandlingMethod => (Func<CommandConsole, int>)(console =>
    {
        console.Error.WriteLine("oops");
        return 2;
    });
}

public sealed class SessionUser
{
    public string Name { get; set; } = "anonymous";
}

public static class RemoteSessionCommands
{
    [Command]
    public static int Login(SessionUser user)
    {
        user.Name = "admin";
        return 0;
    }

    [Command]
    public static int Whoami(CommandConsole console, SessionUser user)
    {
        console.Out.WriteLine(user.Name);
        return 0;
    }
}
