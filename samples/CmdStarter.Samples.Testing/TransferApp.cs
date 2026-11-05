using CyberInternauts.CmdStarter.Samples.Testing.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter.Samples.Testing;

public static class TransferApp
{
    public static CmdStarterBuilder CreateBuilder()
    {
        var builder = new CmdStarterBuilder().AddCommandsFromAssembly(typeof(TransferApp).Assembly);
        builder.Services.AddSingleton<IAccounts, InMemoryAccounts>();
        return builder;
    }
}

