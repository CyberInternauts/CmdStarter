using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter.Tests.App.Options;
using CyberInternauts.CmdStarter.Tests.App.Services;

namespace CyberInternauts.CmdStarter.Tests.App.Methods;

[Command("users")]
public sealed class UserCommands
{
    [Command]
    public int Create(string name, [Option] int age = 30, IGreetingFormatter formatter = null!, AppGlobalOptions globals = null!)
    {
        Console.Out.WriteLine(formatter!.Format(name) + $" ({age})");
        if (globals!.Verbose) Console.Out.WriteLine("verbose");
        return 0;
    }

    [Command]
    public async Task<int> DeleteAsync(int id)
    {
        await Task.Yield();
        return id;
    }

    [Command]
    public static void Fail() => throw new InvalidOperationException("integration failure");

    [Command]
    public static int Scope(RequestContext first, RequestContext second)
    {
        Console.Out.WriteLine(first.Id == second.Id ? "same-scope" : "different-scope");
        return 0;
    }
}
