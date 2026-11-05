using CyberInternauts.CmdStarter.Tests.Commands.GlobalOptions;
using CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;
using System.ComponentModel.DataAnnotations;

namespace CyberInternauts.CmdStarter.Tests.Commands.PlainCommands;

[Command]
public sealed class OptionsPlain
{
    public string Greeting { get; set; } = "Hello";

    public int Times { get; set; } = 1;

    public bool Loud { get; set; }

    public int Run(string name)
    {
        PlainLog.Entries.Add($"{Greeting}:{name}:{Times}:{Loud}");
        return 31;
    }
}

public abstract class PlainBase
{
    public string Inherited { get; set; } = "base";
}

[Command]
public sealed class InheritingPlain : PlainBase
{
    public int Own { get; set; }

    public int Run()
    {
        PlainLog.Entries.Add($"{Inherited}:{Own}");
        return 32;
    }
}

[Command]
public sealed class ExcludedPlain
{
    public string Shown { get; set; } = "shown";

    [NotOption]
    public string Excluded { get; set; } = "excluded";

    public int Run() => 33;
}

[Command]
public sealed class ArgumentsPlain
{
    public int Run([Description("The text")] string text, int count = 2)
    {
        PlainLog.Entries.Add($"{text}:{count}");
        return count;
    }
}

[Command]
public sealed class TokenAndConsolePlain
{
    public bool Wait { get; set; }

    public async Task<int> Run(CancellationToken cancellationToken, CommandConsole console)
    {
        console.Out.WriteLine("plain console");
        if (Wait)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        return 0;
    }
}

[Command]
public sealed class ContainerPlain
{
    public int Run(MainGlobalOptions globals) => globals.IntGlobalOption;
}

[Command]
public sealed class ServicesPlain
{
    public int Run([FromServices] TransientService fromServices, [Option] int level, ScopedService implicitService, string name)
    {
        PlainLog.Received.Add(fromServices);
        PlainLog.Received.Add(implicitService);
        PlainLog.Entries.Add($"{name}:{level}");
        return level;
    }
}

[Command]
public sealed class ValidatedPlain
{
    [Range(1, 5)]
    public int Level { get; set; }

    public int Run([MaxLength(3)] string code) => 0;
}
