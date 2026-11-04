namespace com.cyberinternauts.csharp.CmdStarter.Samples.Testing.Services;

public interface IAccounts
{
    bool Transfer(string from, string to, int amount);
}

