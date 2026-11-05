using CyberInternauts.CmdStarter.Samples.Testing.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;

namespace CyberInternauts.CmdStarter.Samples.Testing.Tests;

public sealed class TransferTests
{
    [Test]
    public async Task TransfersThroughTheAccountsService()
    {
        var builder = TransferApp.CreateBuilder();
        var fake = new FakeAccounts(true);
        builder.Services.Replace(ServiceDescriptor.Singleton(typeof(IAccounts), fake));
        await using var app = builder.Build();
        var result = await app.RunAsync(["transfer", "alice", "bob", "--amount", "10"]);
        Assert.That(result, Is.Zero);
        Assert.That(fake.Calls, Is.EqualTo(new[] { ("alice", "bob", 10) }));
    }

    [Test]
    public async Task ReturnsOneWhenTheTransferIsRefused()
    {
        var builder = TransferApp.CreateBuilder();
        var fake = new FakeAccounts(false);
        builder.Services.Replace(ServiceDescriptor.Singleton(typeof(IAccounts), fake));
        await using var app = builder.Build();
        var result = await app.RunAsync(["transfer", "alice", "bob", "--amount", "10"]);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task ReturnsTwoForANonPositiveAmount()
    {
        var builder = TransferApp.CreateBuilder();
        var fake = new FakeAccounts(true);
        builder.Services.Replace(ServiceDescriptor.Singleton(typeof(IAccounts), fake));
        await using var app = builder.Build();
        var result = await app.RunAsync(["transfer", "alice", "bob", "--amount", "0"]);
        Assert.That(result, Is.EqualTo(2));
        Assert.That(fake.Calls, Is.Empty);
    }
}

