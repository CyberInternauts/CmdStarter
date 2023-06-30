namespace com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces
{
    public interface IOptByAttribute
    {
        static abstract string[] IncludedOptions { get; }
        static abstract string[] ExcludedOptions { get; }
    }
}
