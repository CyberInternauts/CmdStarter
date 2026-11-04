// Sample: an app whose composition is reused by its tests to swap a service for a fake.

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

