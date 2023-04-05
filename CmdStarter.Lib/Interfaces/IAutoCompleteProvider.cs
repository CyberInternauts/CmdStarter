namespace com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces
{
    public interface IAutoCompleteProvider
    {
        string[] GetAutoCompletes();

        static abstract IAutoCompleteProvider GetInstance();
    }
}
