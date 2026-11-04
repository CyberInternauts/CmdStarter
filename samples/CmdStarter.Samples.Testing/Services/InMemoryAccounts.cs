namespace com.cyberinternauts.csharp.CmdStarter.Samples.Testing.Services;

public sealed class InMemoryAccounts : IAccounts
{
    private readonly Dictionary<string, int> balances = new(StringComparer.Ordinal) { ["alice"] = 100, ["bob"] = 50 };
    public bool Transfer(string from, string to, int amount)
    {
        if (!balances.TryGetValue(from, out var sourceBalance) || !balances.ContainsKey(to) || sourceBalance < amount) return false;
        balances[from] -= amount;
        balances[to] += amount;
        return true;
    }
}
