using System.Diagnostics.CodeAnalysis;

namespace CyberInternauts.CmdStarter.Sequences;

/// <summary>How a step of a <see cref="CommandSequence"/> is bound to the step before it.</summary>
public enum SequenceOperator
{
    /// <summary>The first step: nothing precedes it.</summary>
    None,

    /// <summary>The step follows a <c>;</c>: it always runs.</summary>
    Always,

    /// <summary>The step follows a <c>&amp;&amp;</c>: it runs only if the last executed command returned 0.</summary>
    OnSuccess,

    /// <summary>The step follows a <c>||</c>: it runs only if the last executed command did not return 0.</summary>
    OnFailure
}

/// <summary>One command line of a <see cref="CommandSequence"/> and the way it is bound to the previous step.</summary>
/// <param name="CommandLine">The trimmed text of the step, quotes included.</param>
/// <param name="Operator">How this step is bound to the previous one.</param>
public sealed record CommandSequenceStep(string CommandLine, SequenceOperator Operator);

/// <summary>
/// A line split into command lines joined by <c>;</c>, <c>&amp;&amp;</c> and <c>||</c>.
/// Operators only count outside double quotes, which stay in the text of the step; a single <c>|</c> or <c>&amp;</c> is text.
/// </summary>
public sealed class CommandSequence
{
    // Exit code of a cancelled command: it stops the sequence.
    private const int CancelledExitCode = 130;

    private CommandSequence(IReadOnlyList<CommandSequenceStep> steps)
    {
        Steps = steps;
    }

    /// <summary>Gets the steps, from left to right. A blank line has none.</summary>
    public IReadOnlyList<CommandSequenceStep> Steps { get; }

    /// <summary>Splits a line into steps.</summary>
    /// <param name="line">The line to split.</param>
    /// <param name="sequence">The sequence when the line is valid, otherwise <see langword="null"/>.</param>
    /// <param name="error">The reason why the line is invalid, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the line is valid; a blank line is valid and has no step.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="line"/> is null.</exception>
    public static bool TryParse(string line, [NotNullWhen(true)] out CommandSequence? sequence, [NotNullWhen(false)] out string? error)
    {
        ArgumentNullException.ThrowIfNull(line);

        var (operators, hasUnclosedQuote) = Scan(line);
        var steps = new List<CommandSequenceStep>();
        var pending = SequenceOperator.None;
        var segmentStart = 0;

        foreach (var token in operators)
        {
            var text = line[segmentStart..token.Index].Trim();
            error = text.Length == 0 ? CheckEmptyStep(token.Symbol, token.Kind, pending, steps.Count) : null;
            if (error is not null)
            {
                sequence = null;
                return false;
            }

            if (text.Length > 0)
            {
                steps.Add(new CommandSequenceStep(text, pending));
                pending = token.Kind;
            }

            segmentStart = token.Index + token.Symbol.Length;
        }

        if (hasUnclosedQuote)
        {
            sequence = null;
            error = "Unclosed double quote.";
            return false;
        }

        var lastText = line[segmentStart..].Trim();
        if (lastText.Length > 0)
        {
            steps.Add(new CommandSequenceStep(lastText, pending));
        }
        else if (pending is SequenceOperator.OnSuccess or SequenceOperator.OnFailure)
        {
            sequence = null;
            error = $"Missing command after '{Symbol(pending)}'.";
            return false;
        }

        sequence = new CommandSequence(steps);
        error = null;
        return true;
    }

    /// <summary>Tells whether a line contains at least one operator outside double quotes, even when the line is not a valid sequence.</summary>
    /// <param name="line">The line to read.</param>
    /// <returns><see langword="true"/> when <c>;</c>, <c>&amp;&amp;</c> or <c>||</c> is found outside double quotes.</returns>
    internal static bool HasOperator(string line) => Scan(line).Operators.Count > 0;

    /// <summary>
    /// Runs the steps from left to right. The first step and the steps after <c>;</c> always run; a step after <c>&amp;&amp;</c> runs if the last executed
    /// code is 0; a step after <c>||</c> runs if it is not; a skipped step does not change the last executed code.
    /// </summary>
    /// <param name="runStep">Runs a step and returns its exit code, or <see langword="null"/> to stop the sequence.</param>
    /// <returns>The exit code of the last executed step, or 0 when none was executed. A step that returns 130 (cancelled) stops the sequence.</returns>
    internal async Task<int> ExecuteAsync(Func<string, Task<int?>> runStep)
    {
        var lastCode = 0;
        foreach (var step in Steps)
        {
            var mustRun = step.Operator switch
            {
                SequenceOperator.OnSuccess => lastCode == 0,
                SequenceOperator.OnFailure => lastCode != 0,
                _ => true
            };
            if (!mustRun)
            {
                continue;
            }

            var code = await runStep(step.CommandLine);
            if (code is null)
            {
                return lastCode;
            }

            lastCode = code.Value;
            if (lastCode == CancelledExitCode)
            {
                break;
            }
        }

        return lastCode;
    }

    /// <summary>
    /// Finds the step that contains a cursor position, in a line that may be unfinished (a trailing operator, an open quote).
    /// The step starts just after the last operator before the cursor and ends at the next operator or at the end of the line.
    /// </summary>
    /// <param name="line">The line being edited.</param>
    /// <param name="position">The cursor position in <paramref name="line"/>.</param>
    /// <returns>The start (inclusive) and the end (exclusive) of the step, or <see langword="null"/> when the cursor is inside an operator.</returns>
    internal static (int Start, int End)? FindStep(string line, int position)
    {
        var start = 0;
        var end = line.Length;
        foreach (var token in Scan(line).Operators)
        {
            var tokenEnd = token.Index + token.Symbol.Length;
            if (tokenEnd <= position)
            {
                start = tokenEnd;
            }
            else if (token.Index >= position)
            {
                end = token.Index;
                break;
            }
            else
            {
                return null;
            }
        }

        return (start, end);
    }

    // The single reading of quotes and operators, shared by TryParse and FindStep.
    // A double quote opens or closes a quoted zone (no escape); outside quotes, "&&" and "||" (two characters) are read first, then ";".
    private static (List<OperatorToken> Operators, bool HasUnclosedQuote) Scan(string line)
    {
        var operators = new List<OperatorToken>();
        var inQuotes = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (inQuotes)
            {
                continue;
            }

            var next = index + 1 < line.Length ? line[index + 1] : '\0';
            var (symbol, kind) = ReadOperator(character, next);
            if (symbol is null)
            {
                continue;
            }

            operators.Add(new OperatorToken(index, symbol, kind));
            index += symbol.Length - 1;
        }

        return (operators, inQuotes);
    }

    private static (string? Symbol, SequenceOperator Kind) ReadOperator(char character, char next) => (character, next) switch
    {
        ('&', '&') => ("&&", SequenceOperator.OnSuccess),
        ('|', '|') => ("||", SequenceOperator.OnFailure),
        (';', _) => (";", SequenceOperator.Always),
        _ => (null, SequenceOperator.None)
    };

    // An operator that follows an empty step: ";" is allowed (the empty step is ignored) unless a "&&" or "||" is waiting for its command.
    private static string? CheckEmptyStep(string symbol, SequenceOperator kind, SequenceOperator pending, int stepCount)
    {
        if (pending is SequenceOperator.OnSuccess or SequenceOperator.OnFailure)
        {
            return $"Missing command after '{Symbol(pending)}'.";
        }

        if (kind == SequenceOperator.Always)
        {
            return null;
        }

        return stepCount == 0
            ? $"Unexpected '{symbol}' at the start of the line."
            : $"Unexpected '{symbol}' after ';'.";
    }

    private static string Symbol(SequenceOperator kind) => kind == SequenceOperator.OnSuccess ? "&&" : "||";

    private sealed record OperatorToken(int Index, string Symbol, SequenceOperator Kind);
}
