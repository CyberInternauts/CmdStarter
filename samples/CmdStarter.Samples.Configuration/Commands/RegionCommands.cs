using CyberInternauts.CmdStarter.Attributes;
using Microsoft.Extensions.Configuration;

namespace CyberInternauts.CmdStarter.Samples.Configuration.Commands;

public sealed class RegionCommands
{
    [Command]
    public static void ShowRegion(IConfiguration configuration, [Option] string? region = null)
    {
        Console.Out.WriteLine($"Region: {region ?? configuration["Region"] ?? "local"}");
    }
}

