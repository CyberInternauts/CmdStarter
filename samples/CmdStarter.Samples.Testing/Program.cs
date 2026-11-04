using com.cyberinternauts.csharp.CmdStarter.Samples.Testing;
namespace com.cyberinternauts.csharp.CmdStarter.Samples.Testing;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        await using var app = TransferApp.CreateBuilder().Build();
        return await app.RunAsync(args);
    }
}
