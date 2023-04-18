namespace com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces
{
    public interface IHasAliases
    {
        string[] ExpectedOptionAliases { get; }
        string[] ExpectedCommandAliases { get; }
    }
}
