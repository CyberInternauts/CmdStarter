namespace CyberInternauts.CmdStarter.Exceptions;

/// <summary>
/// No command found exception after applying a <see cref="Filter"/>
/// </summary>
public sealed class NoCommandFoundException : CmdStarterException
{
    /// <summary>
    /// Possible filters
    /// </summary>
    public enum Filter : byte
    {
        /// <summary>
        /// Namespaces filter
        /// </summary>
        Namespaces,

        /// <summary>
        /// Classes filter
        /// </summary>
        Classes,

        /// <summary>Assemblies filter</summary>
        Assemblies
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
        : base($"No command was found after applying the {lastFilterApplied} filter.")
    {
        LastFilterApplied = lastFilterApplied;
    }

}
