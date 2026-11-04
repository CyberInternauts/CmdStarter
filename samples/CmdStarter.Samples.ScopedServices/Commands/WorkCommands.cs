using CyberInternauts.CmdStarter;
using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter.Samples.ScopedServices.Services;

namespace CyberInternauts.CmdStarter.Samples.ScopedServices.Commands;

public sealed class WorkCommands
{
    [Command]
    public static int Run(RequestContext context, AuditLog audit, UnitOfWork work)
    {
        Console.Out.WriteLine("working");
        Console.Out.WriteLine(ReferenceEquals(context, audit.Context) ? "same-scope" : "different-scope");
        work.Commit();
        return 0;
    }
}

