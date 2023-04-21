namespace com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions
{
    public class NoCommandFoundException : Exception
    {
        public enum Filters
        {
            Namespaces,
            Classes
        }

        public Filters LastFilterApplied { get; init; }

        public NoCommandFoundException(Filters lastFilterApplied)
        {
            LastFilterApplied = lastFilterApplied;
        }

    }
}
