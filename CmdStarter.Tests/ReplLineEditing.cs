using CyberInternauts.CmdStarter.Repl;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("REPL")]
public class ReplLineEditing
{
    private StringWriter editorOutput = new();

    [Test]
    public void TypingAndEnterReturnsLine()
    {
        var editor = CreateEditor('a', 'b', 'c', Key(ConsoleKey.Enter));

        Assert.That(editor.ReadLine("> "), Is.EqualTo("abc"));
        Assert.That(editorOutput.ToString(), Does.StartWith("> "));
    }

    [Test]
    public void EditingKeysChangeBuffer()
    {
        var editor = CreateEditor('a', 'b', 'c', Key(ConsoleKey.LeftArrow), Key(ConsoleKey.LeftArrow), Key(ConsoleKey.Backspace), 'X', Key(ConsoleKey.End), 'Z', Key(ConsoleKey.Home), Key(ConsoleKey.Delete), Key(ConsoleKey.Enter));

        Assert.That(editor.ReadLine("> "), Is.EqualTo("bcZ"));
    }

    [Test]
    public void HistoryNavigation()
    {
        var editor = CreateEditor('o', 'n', 'e', Key(ConsoleKey.Enter), 't', 'w', 'o', Key(ConsoleKey.Enter), Key(ConsoleKey.UpArrow), Key(ConsoleKey.Enter), Key(ConsoleKey.UpArrow), Key(ConsoleKey.UpArrow), Key(ConsoleKey.DownArrow), Key(ConsoleKey.DownArrow), 'x', Key(ConsoleKey.UpArrow), Key(ConsoleKey.DownArrow), Key(ConsoleKey.Enter));

        Assert.That(editor.ReadLine("> "), Is.EqualTo("one"));
        Assert.That(editor.ReadLine("> "), Is.EqualTo("two"));
        Assert.That(editor.ReadLine("> "), Is.EqualTo("two"));

        var outputLengthBeforeFourthRead = editorOutput.ToString().Length;
        Assert.That(editor.ReadLine("> "), Is.EqualTo("x"));

        var fourthReadOutput = editorOutput.ToString()[outputLengthBeforeFourthRead..];
        Assert.That(RenderedBuffers(fourthReadOutput, "> "), Is.EqualTo(new[] { "two", "one", "two", string.Empty, "x", "two", "x" }));
    }

    [Test]
    public void HistorySkipsBlankAndRepeatedLines()
    {
        var editor = CreateEditor('o', 'n', 'e', Key(ConsoleKey.Enter), ' ', Key(ConsoleKey.Enter), 'o', 'n', 'e', Key(ConsoleKey.Enter), 't', 'w', 'o', Key(ConsoleKey.Enter));

        editor.ReadLine("> ");
        editor.ReadLine("> ");
        editor.ReadLine("> ");
        editor.ReadLine("> ");

        Assert.That(editor.History, Is.EqualTo(new[] { "one", "two" }));
    }

    [Test]
    public void HistoryIsBounded()
    {
        var editor = CreateEditorWithHistory(2, 'a', Key(ConsoleKey.Enter), 'b', Key(ConsoleKey.Enter), 'c', Key(ConsoleKey.Enter));

        editor.ReadLine("> ");
        editor.ReadLine("> ");
        editor.ReadLine("> ");

        Assert.That(editor.History, Is.EqualTo(new[] { "b", "c" }));
    }

    [Test]
    public void TabCompletesSingleCandidate()
    {
        var editor = CreateEditor('s', 't', 'a', Key(ConsoleKey.Tab), Key(ConsoleKey.Enter));
        editor.CompletionSource = (_, _) => ["status", "stop"];

        Assert.That(editor.ReadLine("> "), Is.EqualTo("status "));
    }

    [Test]
    public void TabCompletesCommonPrefixThenListsCandidates()
    {
        var editor = CreateEditor('s', Key(ConsoleKey.Tab), Key(ConsoleKey.Tab), Key(ConsoleKey.Enter));
        editor.CompletionSource = (_, _) => ["status", "stop"];

        Assert.That(editor.ReadLine("> "), Is.EqualTo("st"));
        Assert.That(editorOutput.ToString(), Does.Contain("status  stop"));
    }

    [Test]
    public void EndOfInputReturnsNull()
    {
        var editor = CreateEditor(Key(ConsoleKey.D, control: true), 'd', 'z', Key(ConsoleKey.D, control: true), Key(ConsoleKey.Z, control: true), Key(ConsoleKey.Enter));

        Assert.That(editor.ReadLine("> "), Is.Null);
        Assert.That(editor.ReadLine("> "), Is.EqualTo("dz"));
    }

    // Each render is "\r" + prompt + buffer + padding spaces + backspaces, so splitting on "\r" isolates the buffers.
    private static string[] RenderedBuffers(string readOutput, string prompt)
    {
        return readOutput
            .TrimEnd('\r', '\n')
            .Split('\r')
            .Skip(1)
            .Select(render => render[prompt.Length..].TrimEnd(' ', '\b'))
            .ToArray();
    }

    private ReplLineEditor CreateEditor(params object[] keys) => CreateEditorWithHistory(100, keys);

    private ReplLineEditor CreateEditorWithHistory(int historySize, params object[] keys)
    {
        editorOutput = new StringWriter();
        var pendingKeys = new Queue<ConsoleKeyInfo>(keys.Select(key => key is char character ? Key(character) : (ConsoleKeyInfo)key));
        return new ReplLineEditor(() => pendingKeys.Dequeue(), editorOutput) { HistorySize = historySize };
    }

    private static ConsoleKeyInfo Key(ConsoleKey key, bool control = false) => new('\0', key, false, false, control);

    private static ConsoleKeyInfo Key(char character)
    {
        var key = character switch
        {
            >= 'a' and <= 'z' => (ConsoleKey)char.ToUpperInvariant(character),
            >= 'A' and <= 'Z' => (ConsoleKey)character,
            >= '0' and <= '9' => (ConsoleKey)character,
            ' ' => ConsoleKey.Spacebar,
            _ => ConsoleKey.NoName,
        };
        return new(character, key, false, false, false);
    }
}
