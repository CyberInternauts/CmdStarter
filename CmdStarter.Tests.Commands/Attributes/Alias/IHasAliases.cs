namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Alias
{
    public interface IHasAliases
    {
        string[] ExpectedOptionAliases { get; }
        string[] ExpectedCommandAliases { get; }
    }
}
