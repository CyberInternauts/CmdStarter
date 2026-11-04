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

// Writes its argument as it received it, so a test sees what a line became after the splitting of the REPL and of System.CommandLine.
public sealed class Echo : StarterCommand
{
    public override Delegate HandlingMethod => (Func<CommandConsole, string, int>)((console, text) =>
    {
        console.Out.WriteLine(text);
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

// Writes to the console in pieces: single characters, a null string, a line ended by CR LF, and lines that are never ended.
public sealed class Fragments : StarterCommand
{
    public override Delegate HandlingMethod => (Func<CommandConsole, int>)(console =>
    {
        console.Out.Write('x');
        console.Out.Write('y');
        console.Out.Write('\n');
        console.Out.Write('\n');
        console.Out.Write((string?)null);
        console.Out.Write("crlf\r\n");
        console.Out.Write("tail");
        console.Error.Write("etail");
        return 0;
    });
}

public sealed class EncodingName : StarterCommand
{
    public override Delegate HandlingMethod => (Func<CommandConsole, int>)(console =>
    {
        console.Out.WriteLine(console.Out.Encoding.WebName);
        return 0;
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
