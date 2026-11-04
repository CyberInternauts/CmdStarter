namespace CyberInternauts.CmdStarter.Samples.ScopedServices.Services;

public sealed class AuditLog(RequestContext context)
{
    public RequestContext Context { get; } = context;
}

