using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using System.ComponentModel;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Methods;

[Command("users")]
public sealed class UserCommands : IDisposable
{
    public const string CREATE_DESC = "Create a user";
    public const string NAME_DESC = "User name";

    [Command("create")]
    [Description(CREATE_DESC)]
    public int Create([Description(NAME_DESC)] string name, [Option] int age = 30, [Option] bool admin = false)
    { MethodsLog.Entries.Add($"create:{name}:{age}:{admin}"); return 10; }

    [Command]
    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken)
    { await Task.Yield(); MethodsLog.Entries.Add($"delete:{id}"); return 20; }

    [Command]
    public static void List() => MethodsLog.Entries.Add("list");

    public void NotACommand() { }
    public void Dispose() => MethodsLog.Entries.Add("dispose");
}
