using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.ReplApp;

public sealed class ReplAppCommands
{
    [Command]
    public static int Probe(ScopeProbe probe, ProbeLog log)
    {
        log.Ids.Add(probe.Id);
        return 0;
    }
}
