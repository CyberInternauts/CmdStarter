namespace com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions
{
    /// <summary>
    /// No command found exception after applying a <see cref="Filter"/>
    /// </summary>
    public class NoCommandFoundException : Exception
    {
        /// <summary>
        /// Possible filters
        /// </summary>
        public enum Filter
        {
            /// <summary>
            /// Namespaces filter
            /// </summary>
            Namespaces,

            /// <summary>
            /// Classes filter
            /// </summary>
            Classes
        }

        /// <summary>
        /// Last filter applied
        /// </summary>
        public Filter LastFilterApplied { get; init; }

        /// <summary>
        /// Constructor with <see cref="Filter"/> that was applied to obtained no command
        /// </summary>
        /// <param name="lastFilterApplied"></param>
        public NoCommandFoundException(Filter lastFilterApplied)
        {
            LastFilterApplied = lastFilterApplied;
        }

    }
}
