namespace com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions
{
    public class NoCommandFoundException : Exception
    {
        public enum Filter
        {
            Namespaces,
            Classes
        }

        public Filter LastFilterApplied { get; init; }

        public NoCommandFoundException(Filter lastFilterApplied)
        {
            LastFilterApplied = lastFilterApplied;
        }

    }
}
