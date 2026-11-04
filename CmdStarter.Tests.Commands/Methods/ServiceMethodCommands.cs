using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Lifecycle;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Methods;

[Command("svc")]
public sealed class ServiceMethodCommands(SingletonService singleton)
{
    public SingletonService Singleton { get; } = singleton;
    public static List<object> Received { get; } = new();

    [Command]
    public int Inferred(string name, ScopedService scoped, ScopedConsumer consumer)
    { Received.Add(scoped); Received.Add(consumer); return 0; }

    [Command]
    public int Explicit([FromServices] TransientService transient)
    { Received.Add(transient); return 0; }

    [Command]
    public static int StaticWithService(ScopedService scoped)
    { Received.Add(scoped); return 0; }
}
