namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces
{
    public interface IHasAliases
    {
        string[] ExpectedOptionAliases { get; }
        string[] ExpectedCommandAliases { get; }
    }
}
