using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Repl;

/// <summary>
/// Provides command inputs from the default console.
/// </summary>
public sealed class ConsoleReplProvider : IReplInputProvider
{
    /// <summary>Gets or sets the prompt written before each input line.</summary>
    public string Prompt { get; init; } = "> ";

    /// <summary>
    /// Writes the prompt and returns an input from the default console.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the input operation.</param>
    /// <returns>The next input line, or <see langword="null"/> at end of input.</returns>
    public async ValueTask<string?> GetInputAsync(CancellationToken cancellationToken)
    {
        Console.Out.Write(Prompt);
        return await Console.In.ReadLineAsync(cancellationToken);
    }
}
