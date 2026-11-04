using System.Diagnostics.CodeAnalysis;
using System.Text;

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

        var steps = new List<CommandSequenceStep>();
        var current = new StringBuilder();
        var pending = SequenceOperator.None;
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                inQuotes = !inQuotes;
                current.Append(character);
                continue;
            }

            var next = index + 1 < line.Length ? line[index + 1] : '\0';
            var (symbol, kind) = inQuotes ? (null, SequenceOperator.None) : ReadOperator(character, next);
            if (symbol is null)
            {
                current.Append(character);
                continue;
            }

            var text = current.ToString().Trim();
            current.Clear();
            error = text.Length == 0 ? CheckEmptyStep(symbol, kind, pending, steps.Count) : null;
            if (error is not null)
            {
                sequence = null;
                return false;
            }

            if (text.Length > 0)
            {
                steps.Add(new CommandSequenceStep(text, pending));
                pending = kind;
            }

            if (symbol.Length == 2)
            {
                index++;
            }
        }

        if (inQuotes)
        {
            sequence = null;
            error = "Unclosed double quote.";
            return false;
        }

        var lastText = current.ToString().Trim();
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

    // Reads an operator at the current character: "&&" and "||" (two characters) first, then ";".
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
}
