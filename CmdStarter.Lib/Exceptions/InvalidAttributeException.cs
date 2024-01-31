namespace com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions
{
    /// <summary>
    /// Attribute is not valid exception
    /// </summary>
    public sealed class InvalidAttributeException : Exception
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public InvalidAttributeException() { }

        /// <summary>
        /// Constructor with a specific message
        /// </summary>
        /// <param name="message"></param>
        public InvalidAttributeException(string message) : base(message) { }
    }
}
