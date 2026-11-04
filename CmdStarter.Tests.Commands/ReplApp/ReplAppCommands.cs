namespace CyberInternauts.CmdStarter.Tests.Commands.ReplApp;

public sealed class ReplAppCommands
{
    [Command]
    public static int Probe(ScopeProbe probe, ProbeLog log)
    {
        log.Ids.Add(probe.Id);
        return 0;
    }
}
