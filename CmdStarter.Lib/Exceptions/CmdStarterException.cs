namespace com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions;

/// <summary>
/// Base type for CmdStarter configuration errors. These exceptions are thrown while the command tree is built and are not converted to exit codes.
/// </summary>
public class CmdStarterException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CmdStarterException"/> class.
    /// </summary>
    public CmdStarterException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CmdStarterException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public CmdStarterException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CmdStarterException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public CmdStarterException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
