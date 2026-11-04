using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Repl;

/// <summary>
/// Provides command inputs from the default console.
/// </summary>
public sealed class ConsoleReplProvider : IReplInputProvider
{
    private int historySize = 100;
    private ReplLineEditor? editor;

    /// <summary>Gets or sets the prompt written before each input line.</summary>
    public string Prompt { get; init; } = "> ";

    /// <summary>
    /// Gets or initializes whether lines are edited with <see cref="ReplLineEditor"/> (history and completion).
    /// Editing needs a terminal on both sides: it is skipped when the standard input or the standard output is redirected,
    /// and the line is then read as is.
    /// </summary>
    public bool LineEditing { get; init; } = true;

    /// <summary>Gets or initializes the maximum number of lines kept in the history of the session.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than one.</exception>
    public int HistorySize
    {
        get => historySize;
        init => historySize = value >= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the source of completion candidates, which receives the line and the cursor position.
    /// <see cref="ReplStarter"/> sets it when it is not set yet.
    /// </summary>
    public Func<string, int, IEnumerable<string>>? CompletionSource { get; set; }

    /// <summary>
    /// Writes the prompt and returns an input from the default console.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the input operation.</param>
    /// <returns>The next input line, or <see langword="null"/> at end of input.</returns>
    public async ValueTask<string?> GetInputAsync(CancellationToken cancellationToken)
    {
        if (LineEditing && !Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            editor ??= new ReplLineEditor(() => Console.ReadKey(intercept: true), Console.Out) { HistorySize = HistorySize };
            editor.CompletionSource = CompletionSource;
            return editor.ReadLine(Prompt);
        }

        Console.Out.Write(Prompt);
        return await Console.In.ReadLineAsync(cancellationToken);
    }
}
