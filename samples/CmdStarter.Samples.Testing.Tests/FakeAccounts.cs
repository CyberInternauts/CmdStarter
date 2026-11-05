using CyberInternauts.CmdStarter.Samples.Testing.Services;

namespace CyberInternauts.CmdStarter.Samples.Testing.Tests;

public sealed class FakeAccounts(bool accept) : IAccounts
{
    public List<(string From, string To, int Amount)> Calls { get; } = [];

    public bool Transfer(string from, string to, int amount)
    {
        Calls.Add((from, to, amount));
        return accept;
    }
}

