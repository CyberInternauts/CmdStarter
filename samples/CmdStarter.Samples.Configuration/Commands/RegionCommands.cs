using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using Microsoft.Extensions.Configuration;
namespace com.cyberinternauts.csharp.CmdStarter.Samples.Configuration.Commands;

public sealed class RegionCommands
{
    [Command]
    public static void ShowRegion(IConfiguration configuration, [Option] string? region = null) => Console.Out.WriteLine($"Region: {region ?? configuration["Region"] ?? "local"}");
}
