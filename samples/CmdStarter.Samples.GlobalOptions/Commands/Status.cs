namespace CyberInternauts.CmdStarter.Samples.GlobalOptions.Commands;

public sealed class Status : StarterCommand
{
    public override Delegate HandlingMethod => (Action)(() =>
    {
        Console.Out.WriteLine("status: ok");
        if (GetGlobalOptions<Options.OutputOptions>()?.Verbose == true)
        {
            Console.Out.WriteLine("details: all systems nominal");
        }
    });
}

