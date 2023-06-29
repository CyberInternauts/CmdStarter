namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options.Attributes
{
    public interface IOptByAttribute
    {
        static abstract string[] IncludedOptions { get; }
        static abstract string[] ExcludedOptions { get; }
    }
}
