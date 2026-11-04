using CyberInternauts.CmdStarter.Repl;

namespace CyberInternauts.CmdStarter.Tests;

/// <summary>Edges of <see cref="ReplLineEditor"/> that the main editing tests do not need: limits, empty completions and unusual keys.</summary>
[Category("* All *")]
[Category("REPL")]
public class ReplLineEditorEdges
{
    private StringWriter editorOutput = new();

    [Test]
    public void RightArrowMovesTheCursorAndStopsAtTheEndOfTheLine()
    {
        var editor = CreateEditor('a', 'b', Key(ConsoleKey.LeftArrow), Key(ConsoleKey.LeftArrow), Key(ConsoleKey.RightArrow), 'X', Key(ConsoleKey.RightArrow), Key(ConsoleKey.RightArrow), Key(ConsoleKey.RightArrow), 'Y', Key(ConsoleKey.Enter));

        Assert.That(editor.ReadLine("> "), Is.EqualTo("aXbY"));
    }

    [Test]
    public void TabDoesNothingWithoutACompletionSource()
    {
        var editor = CreateEditor('a', Key(ConsoleKey.Tab), 'b', Key(ConsoleKey.Enter));

        Assert.That(editor.CompletionSource, Is.Null);
        Assert.That(editor.ReadLine("> "), Is.EqualTo("ab"));
    }

    [Test]
    public void TabDoesNothingWhenNoCandidateStartsWithTheWord()
    {
        var editor = CreateEditor('a', Key(ConsoleKey.Tab), Key(ConsoleKey.Enter));
        editor.CompletionSource = (_, _) => ["zeta", "omega"];

        Assert.That(editor.ReadLine("> "), Is.EqualTo("a"));
        Assert.That(editorOutput.ToString(), Does.Not.Contain("zeta").And.Not.Contain("omega"));
    }

    [Test]
    public void HistorySizeMustBeAtLeastOne()
    {
        foreach (var invalid in new[] { 0, -1 })
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _ = new ReplLineEditor(() => default, new StringWriter()) { HistorySize = invalid });
            Assert.That(exception!.ParamName, Is.EqualTo("value"));
        }

        Assert.That(new ReplLineEditor(() => default, new StringWriter()) { HistorySize = 1 }.HistorySize, Is.EqualTo(1));
    }

    [Test]
    public void UpArrowChangesNothingWithoutHistoryAndStopsAtTheOldestLine()
    {
        var withoutHistory = CreateEditor('x', Key(ConsoleKey.UpArrow), Key(ConsoleKey.Enter));
        Assert.That(withoutHistory.ReadLine("> "), Is.EqualTo("x"));

        var withHistory = CreateEditor('o', 'n', 'e', Key(ConsoleKey.Enter), Key(ConsoleKey.UpArrow), Key(ConsoleKey.UpArrow), Key(ConsoleKey.UpArrow), Key(ConsoleKey.Enter));
        Assert.That(withHistory.ReadLine("> "), Is.EqualTo("one"));
        Assert.That(withHistory.ReadLine("> "), Is.EqualTo("one"));
    }

    [Test]
    public void TabCompletesTheLongestCommonPrefixOfTheCandidates()
    {
        var editor = CreateEditor('a', Key(ConsoleKey.Tab), Key(ConsoleKey.Enter));
        editor.CompletionSource = (_, _) => ["abd", "ab", "abc"];
        Assert.That(editor.ReadLine("> "), Is.EqualTo("ab"));

        var other = CreateEditor('a', Key(ConsoleKey.Tab), Key(ConsoleKey.Enter));
        other.CompletionSource = (_, _) => ["alpha", "alps", "alpine"];
        Assert.That(other.ReadLine("> "), Is.EqualTo("alp"));
    }

    [Test]
    public void UnhandledKeysAreIgnoredAndAltGrCharactersAreTyped()
    {
        // F1 has no character; Ctrl+D on a filled line is not an end of input; Ctrl+Alt+D is AltGr, which types its character.
        var altGr = new ConsoleKeyInfo('d', ConsoleKey.D, shift: false, alt: true, control: true);
        var editor = CreateEditor('a', new ConsoleKeyInfo('\0', ConsoleKey.F1, false, false, false), Key(ConsoleKey.D, control: true), altGr, Key(ConsoleKey.Enter));

        Assert.That(editor.ReadLine("> "), Is.EqualTo("ad"));
    }

    private ReplLineEditor CreateEditor(params object[] keys)
    {
        editorOutput = new StringWriter();
        var pendingKeys = new Queue<ConsoleKeyInfo>(keys.Select(key => key is char character ? Key(character) : (ConsoleKeyInfo)key));
        return new ReplLineEditor(() => pendingKeys.Dequeue(), editorOutput);
    }

    private static ConsoleKeyInfo Key(ConsoleKey key, bool control = false) => new('\0', key, false, false, control);

    private static ConsoleKeyInfo Key(char character)
    {
        var key = character switch
        {
            >= 'a' and <= 'z' => (ConsoleKey)char.ToUpperInvariant(character),
            >= 'A' and <= 'Z' => (ConsoleKey)character,
            ' ' => ConsoleKey.Spacebar,
            _ => ConsoleKey.NoName,
        };
        return new(character, key, false, false, false);
    }
}
