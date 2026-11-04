namespace com.cyberinternauts.csharp.CmdStarter.Tests.App.Services;

public sealed class GreetingFormatter : IGreetingFormatter
{
    public string Format(string name) => $"Hello {name}";
}
