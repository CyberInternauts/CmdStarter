using System.Text;

namespace CyberInternauts.CmdStarter.Repl;

/// <summary>
/// Reads and edits one console line, with session history and optional completion.
/// Lines wider than the console are not supported.
/// </summary>
public sealed class ReplLineEditor
{
    private readonly Func<ConsoleKeyInfo> readKey;
    private readonly TextWriter output;
    private readonly List<string> history = [];
    private int historySize = 100;

    /// <summary>Creates a line editor using the supplied key reader and output writer.</summary>
    /// <param name="readKey">Reads the next key.</param>
    /// <param name="output">Receives prompts, edited lines, and completion candidates.</param>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    public ReplLineEditor(Func<ConsoleKeyInfo> readKey, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(readKey);
        ArgumentNullException.ThrowIfNull(output);
        this.readKey = readKey;
        this.output = output;
    }

    /// <summary>Gets or initializes the maximum number of lines retained in history.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than one.</exception>
    public int HistorySize
    {
        get => historySize;
        init => historySize = value >= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    /// <summary>Gets the entered nonblank lines, from oldest to newest.</summary>
    public IReadOnlyList<string> History => history.AsReadOnly();

    /// <summary>
    /// Gets or sets a source of completion candidates for the current line and cursor position.
    /// The current word starts after the last white space, <c>;</c>, <c>&amp;</c> or <c>|</c> before the cursor,
    /// so a candidate that itself contains one of these characters is not completed past it.
    /// </summary>
    public Func<string, int, IEnumerable<string>>? CompletionSource { get; set; }

    /// <summary>Reads an editable line from the key reader.</summary>
    /// <param name="prompt">Prompt written before the line.</param>
    /// <returns>The completed line, or null when end of input is signaled on an empty line.</returns>
    public string? ReadLine(string prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        output.Write(prompt);
        var buffer = new StringBuilder();
        var cursor = 0;
        var renderedLength = 0;
        var historyIndex = history.Count;
        string? draft = null;

        while (true)
        {
            var key = readKey();
            var changed = false;
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    var line = buffer.ToString();
                    output.WriteLine();
                    if (!string.IsNullOrWhiteSpace(line) && (history.Count == 0 || !string.Equals(history[^1], line, StringComparison.Ordinal)))
                    {
                        history.Add(line);
                        if (history.Count > HistorySize)
                        {
                            history.RemoveAt(0);
                        }
                    }

                    return line;
                case ConsoleKey.D or ConsoleKey.Z when IsControlWithoutAlt(key):
                    if (buffer.Length == 0)
                    {
                        return null;
                    }

                    break;
                case ConsoleKey.Backspace:
                    if (cursor > 0)
                    {
                        buffer.Remove(--cursor, 1);
                        changed = true;
                    }

                    break;
                case ConsoleKey.Delete:
                    if (cursor < buffer.Length)
                    {
                        buffer.Remove(cursor, 1);
                        changed = true;
                    }

                    break;
                case ConsoleKey.LeftArrow:
                    cursor = Math.Max(0, cursor - 1);
                    changed = true;
                    break;
                case ConsoleKey.RightArrow:
                    cursor = Math.Min(buffer.Length, cursor + 1);
                    changed = true;
                    break;
                case ConsoleKey.Home:
                    cursor = 0;
                    changed = true;
                    break;
                case ConsoleKey.End:
                    cursor = buffer.Length;
                    changed = true;
                    break;
                case ConsoleKey.Escape:
                    buffer.Clear();
                    cursor = 0;
                    changed = true;
                    break;
                case ConsoleKey.UpArrow:
                    if (history.Count > 0 && historyIndex > 0)
                    {
                        if (historyIndex == history.Count)
                        {
                            draft = buffer.ToString();
                        }

                        buffer.Clear().Append(history[--historyIndex]);
                        cursor = buffer.Length;
                        changed = true;
                    }

                    break;
                case ConsoleKey.DownArrow:
                    if (historyIndex < history.Count)
                    {
                        historyIndex++;
                        buffer.Clear().Append(historyIndex == history.Count ? draft : history[historyIndex]);
                        cursor = buffer.Length;
                        changed = true;
                    }

                    break;
                case ConsoleKey.Tab:
                    changed = Complete(buffer, ref cursor, prompt, ref renderedLength);
                    break;
                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        buffer.Insert(cursor++, key.KeyChar);
                        changed = true;
                    }

                    break;
            }

            if (changed && key.Key != ConsoleKey.Tab)
            {
                Render(prompt, buffer, cursor, ref renderedLength);
            }
        }
    }

    private bool Complete(StringBuilder buffer, ref int cursor, string prompt, ref int renderedLength)
    {
        if (CompletionSource is null)
        {
            return false;
        }

        var start = cursor;
        while (start > 0 && !IsWordBoundary(buffer[start - 1]))
        {
            start--;
        }

        var word = buffer.ToString(start, cursor - start);
        var candidates = CompletionSource(buffer.ToString(), cursor)
            .Where(candidate => candidate.StartsWith(word, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(candidate => candidate, StringComparer.Ordinal)
            .ToArray();
        if (candidates.Length == 0)
        {
            return false;
        }

        if (candidates.Length == 1)
        {
            buffer.Remove(start, cursor - start).Insert(start, candidates[0] + " ");
            cursor = start + candidates[0].Length + 1;
            Render(prompt, buffer, cursor, ref renderedLength);
            return true;
        }

        var prefix = CommonPrefix(candidates);
        if (prefix.Length > word.Length)
        {
            buffer.Remove(start, cursor - start).Insert(start, prefix);
            cursor = start + prefix.Length;
            Render(prompt, buffer, cursor, ref renderedLength);
            return true;
        }

        output.WriteLine();
        output.WriteLine(string.Join("  ", candidates));
        renderedLength = 0;
        Render(prompt, buffer, cursor, ref renderedLength);
        return true;
    }

    private void Render(string prompt, StringBuilder buffer, int cursor, ref int renderedLength)
    {
        output.Write('\r');
        output.Write(prompt);
        output.Write(buffer);
        var extra = Math.Max(0, renderedLength - buffer.Length);
        output.Write(new string(' ', extra));
        var backspaces = buffer.Length - cursor + extra;
        output.Write(new string('\b', backspaces));
        renderedLength = buffer.Length;
    }

    // A word ends at a white space or at a sequence operator character (";", "&", "|").
    private static bool IsWordBoundary(char character) => char.IsWhiteSpace(character) || character is ';' or '&' or '|';

    // Windows reports AltGr as Control+Alt, so a key with Alt is never an end-of-input request.
    private static bool IsControlWithoutAlt(ConsoleKeyInfo key)
    {
        return (key.Modifiers & ConsoleModifiers.Control) != 0 && (key.Modifiers & ConsoleModifiers.Alt) == 0;
    }

    private static string CommonPrefix(string[] candidates)
    {
        if (candidates.Length == 0)
        {
            return string.Empty;
        }

        var prefix = candidates[0];
        for (var index = 1; index < candidates.Length; index++)
        {
            var candidate = candidates[index];
            var length = 0;
            while (length < prefix.Length && length < candidate.Length && prefix[length] == candidate[length])
            {
                length++;
            }

            prefix = prefix[..length];
        }

        return prefix;
    }
}
