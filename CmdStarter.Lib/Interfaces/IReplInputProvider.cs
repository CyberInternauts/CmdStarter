namespace com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

/// <summary>
/// Provides an input for the REPL mode to parse.
/// </summary>
public interface IReplInputProvider
{
    /// <summary>
    /// Returns the next input line, or <see langword="null"/> when input has ended.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the input operation.</param>
    /// <returns>The next input line, or <see langword="null"/> at end of input.</returns>
    public ValueTask<string?> GetInputAsync(CancellationToken cancellationToken);
}
