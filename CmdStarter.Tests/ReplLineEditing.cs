using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Repl;
using CyberInternauts.CmdStarter.Tests.Commands.Attributes.AutoComplete;
using CyberInternauts.CmdStarter.Tests.Commands.Repl;
using CyberInternauts.CmdStarter.Tests.Commands.States;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("REPL")]
public class ReplLineEditing
{
    private const string CommandOneName = "command-one";

    private StringWriter editorOutput = new();

    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

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
    public void CompletesAfterSequenceOperator()
    {
        var editor = CreateEditor('o', 'k', ';', 'o', 't', Key(ConsoleKey.Tab), Key(ConsoleKey.Enter));
        editor.CompletionSource = (_, _) => ["other", "ok"];

        Assert.That(editor.ReadLine("> "), Is.EqualTo("ok;other "));
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

    [Test]
    public void CompletionsListCommandsAndBuiltInWords()
    {
        var completions = CreateReplStarter().GetCompletions(string.Empty, 0);

        Assert.That(completions, Does.Contain(CommandOneName));
        Assert.That(completions, Does.Contain("exit"));
        Assert.That(completions, Does.Contain("help"));
    }

    [Test]
    public void CompletionsAfterHelpWordListCommands()
    {
        var completions = CreateReplStarter().GetCompletions("help ", 5);

        Assert.That(completions, Does.Contain(CommandOneName));
        Assert.That(completions, Does.Not.Contain("exit"));
    }

    [Test]
    public void CompletionsHideUnavailableCommands()
    {
        var provider = new TestStateProvider { CurrentState = TestState.Anonymous };
        var starter = new ReplStarter(new TestInputProvider())
        {
            Namespaces = ImmutableList.Create(typeof(Enter).Namespace!),
            StateProvider = provider
        };

        var anonymous = starter.GetCompletions(string.Empty, 0);
        Assert.That(anonymous, Does.Contain("enter"));
        Assert.That(anonymous, Does.Not.Contain("profile"));

        provider.CurrentState = TestState.Member;
        var member = starter.GetCompletions(string.Empty, 0);
        Assert.That(member, Does.Contain("profile"));
        Assert.That(member, Does.Not.Contain("enter"));
    }

    [Test]
    public void CompletionsIncludeAutoCompleteValues()
    {
        var starter = new ReplStarter(new TestInputProvider())
        {
            Classes = ImmutableList.Create(typeof(SingleSingleAutoComplete).FullName!)
        };
        var line = $"{StarterCommand.OptionPrefix}{SingleSingleAutoComplete.OPTION_NAME} ";

        Assert.That(starter.GetCompletions(line, line.Length), Does.Contain("Bob"));
    }

    [Test]
    public void ConsoleProviderGetsCompletionSource()
    {
        var provider = new ConsoleReplProvider();
        _ = new ReplStarter(provider);
        Assert.That(provider.CompletionSource, Is.Not.Null);

        Func<string, int, IEnumerable<string>> ownSource = (_, _) => [];
        var customProvider = new ConsoleReplProvider { CompletionSource = ownSource };
        _ = new ReplStarter(customProvider);
        Assert.That(customProvider.CompletionSource, Is.SameAs(ownSource));
    }

    [Test]
    public void EditorCompletesFromStarter()
    {
        var starter = CreateReplStarter();
        var editor = CreateEditor('c', 'o', 'm', 'm', 'a', 'n', 'd', '-', 'o', Key(ConsoleKey.Tab), Key(ConsoleKey.Enter));
        editor.CompletionSource = starter.GetCompletions;

        Assert.That(editor.ReadLine("> "), Is.EqualTo(CommandOneName + " "));
    }

    private static ReplStarter CreateReplStarter()
    {
        var starter = new ReplStarter(new TestInputProvider());
        Repl.SetDefaultNamespaces(starter);
        starter.Namespaces = starter.Namespaces.Add(typeof(CommandOne).Namespace!);
        return starter;
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
