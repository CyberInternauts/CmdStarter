using System.Reflection;
using System.Text.RegularExpressions;
using CyberInternauts.CmdStarter.Hosting;

namespace CyberInternauts.CmdStarter.Tests.Integration;

[Category("* All *")]
[Category("Documentation")]
public sealed class DocumentationTests
{
    private static readonly Regex LinkPattern = new(@"\]\(([^)\s]+)(?:\s+""[^""]*"")?\)", RegexOptions.CultureInvariant);
    private static readonly Regex InlineCodeSpan = new(@"`[^`\n]*`", RegexOptions.CultureInvariant);
    private static readonly Regex InlineCodeToken = new(@"`([^`\n]+)`", RegexOptions.CultureInvariant);
    private static readonly Regex SnippetMarker = new(@"^<!-- snippet: (\S+) -->$", RegexOptions.CultureInvariant);
    private static readonly Regex AttributeToken = new(@"^\[(?<name>[A-Za-z_]\w*)(<[^\[\]]*>)?(\(.*\))?\]$", RegexOptions.CultureInvariant);

    private static readonly Lazy<Type[]> LibraryTypes = new(() => LibraryAssemblies().SelectMany(ExportedTypes).Where(type => !type.IsNested).ToArray());
    private static readonly Lazy<HashSet<string>> AttributeNames = new(FindAttributeNames);

    [Test]
    public void RelativeLinksPointToExistingFiles()
    {
        AssertLinkCheckerWorks();

        var failures = new List<string>();
        foreach (var file in ExaminedFiles(includeDecisionRecords: true))
        {
            failures.AddRange(FindBrokenLinks(RelativePath(file), Path.GetDirectoryName(file)!, File.ReadAllLines(file)));
        }

        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void CSharpSnippetsComeFromRepositoryFiles()
    {
        AssertSnippetCheckerWorks();

        var failures = new List<string>();
        foreach (var file in ExaminedFiles(includeDecisionRecords: false))
        {
            failures.AddRange(FindSnippetProblems(RelativePath(file), File.ReadAllLines(file), RepositoryPaths.Root));
        }

        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void DocumentationIdentifiersExist()
    {
        AssertIdentifierCheckerWorks();

        var failures = new List<string>();
        foreach (var file in ExaminedFiles(includeDecisionRecords: false))
        {
            failures.AddRange(FindIdentifierProblems(RelativePath(file), File.ReadAllLines(file)));
        }

        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
    }

    // README.md, PKG-README.md, samples/README.md and every Markdown file under docs/ (the decision records and the migration guide only for links).
    private static IEnumerable<string> ExaminedFiles(bool includeDecisionRecords)
    {
        var root = RepositoryPaths.Root;
        foreach (var name in new[] { "README.md", "PKG-README.md", "samples/README.md" })
        {
            yield return Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar));
        }

        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "docs"), "*.md", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var relative = RelativePath(file);
            var isDecisionRecord = relative.StartsWith("docs/adr/", StringComparison.Ordinal) || relative.StartsWith("docs/migration/", StringComparison.Ordinal);
            if (includeDecisionRecords || !isDecisionRecord)
            {
                yield return file;
            }
        }
    }

    private static string RelativePath(string path) => Path.GetRelativePath(RepositoryPaths.Root, path).Replace('\\', '/');

    // The lines outside fenced code blocks, with their line numbers.
    private static IEnumerable<(int Number, string Text)> ProseLines(string[] lines)
    {
        var inFence = false;
        for (var index = 0; index < lines.Length; index++)
        {
            if (lines[index].TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            if (!inFence)
            {
                yield return (index + 1, lines[index]);
            }
        }
    }

    // ---- Links ----

    private static List<string> FindBrokenLinks(string displayName, string fileDirectory, string[] lines)
    {
        var failures = new List<string>();
        foreach (var (number, text) in ProseLines(lines))
        {
            foreach (Match match in LinkPattern.Matches(InlineCodeSpan.Replace(text, string.Empty)))
            {
                var target = match.Groups[1].Value;
                if (IsBrokenLink(fileDirectory, target))
                {
                    failures.Add($"{displayName}, line {number} : {target}");
                }
            }
        }

        return failures;
    }

    private static bool IsBrokenLink(string fileDirectory, string target)
    {
        if (target.StartsWith("http://", StringComparison.Ordinal)
            || target.StartsWith("https://", StringComparison.Ordinal)
            || target.StartsWith("mailto:", StringComparison.Ordinal)
            || target.StartsWith('#'))
        {
            return false;
        }

        var anchor = target.IndexOf('#', StringComparison.Ordinal);
        var path = Uri.UnescapeDataString(anchor >= 0 ? target[..anchor] : target);
        if (path.Length == 0)
        {
            return false;
        }

        var full = Path.GetFullPath(Path.Combine(fileDirectory, path.Replace('/', Path.DirectorySeparatorChar)));
        return !File.Exists(full) && !Directory.Exists(full);
    }

    // A test that cannot find anything proves nothing: the checker must find a broken link and accept the good ones.
    private static void AssertLinkCheckerWorks()
    {
        var root = RepositoryPaths.Root;
        Assert.Multiple(() =>
        {
            Assert.That(IsBrokenLink(root, "samples/README.md"), Is.False);
            Assert.That(IsBrokenLink(root, "samples/README.md#top"), Is.False);
            Assert.That(IsBrokenLink(root, "samples"), Is.False);
            Assert.That(IsBrokenLink(root, "docs/migration/1.x-to-2.0.md"), Is.False);
            Assert.That(IsBrokenLink(root, "https://example.com/missing"), Is.False);
            Assert.That(IsBrokenLink(root, "mailto:someone@example.com"), Is.False);
            Assert.That(IsBrokenLink(root, "#section"), Is.False);
            Assert.That(IsBrokenLink(root, "docs/does-not-exist.md"), Is.True);

            var failures = FindBrokenLinks(
                "sample.md",
                root,
                ["[good](samples/README.md) and [bad](docs/nope.md)", "```", "[inside a code block](docs/nope2.md)", "```", "`[in code](nope3.md)`"]);
            Assert.That(failures, Is.EqualTo(new[] { "sample.md, line 1 : docs/nope.md" }));
        });
    }

    // ---- C# snippets ----

    private static List<string> FindSnippetProblems(string displayName, string[] lines, string root)
    {
        var failures = new List<string>();
        var inFence = false;
        for (var index = 0; index < lines.Length; index++)
        {
            var opening = lines[index].Trim();
            if (!opening.StartsWith("```", StringComparison.Ordinal))
            {
                continue;
            }

            if (inFence)
            {
                inFence = false;
                continue;
            }

            inFence = true;
            if (!opening.StartsWith("```csharp", StringComparison.Ordinal))
            {
                continue;
            }

            var previous = index - 1;
            while (previous >= 0 && lines[previous].Trim().Length == 0)
            {
                previous--;
            }

            var marker = previous >= 0 ? SnippetMarker.Match(lines[previous].Trim()) : Match.Empty;
            if (!marker.Success)
            {
                failures.Add($"{displayName}, line {index + 1} : the csharp block is not preceded by a '<!-- snippet: path -->' comment");
                continue;
            }

            var snippetPath = marker.Groups[1].Value;
            var snippetFile = Path.Combine(root, snippetPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(snippetFile))
            {
                failures.Add($"{displayName}, line {previous + 1} : snippet file not found: {snippetPath}");
                continue;
            }

            var knownLines = File.ReadAllLines(snippetFile).Select(line => line.Trim()).ToHashSet(StringComparer.Ordinal);
            for (var body = index + 1; body < lines.Length && !lines[body].Trim().StartsWith("```", StringComparison.Ordinal); body++)
            {
                var text = lines[body].Trim();
                if (text.Length > 0 && text != "// ..." && !knownLines.Contains(text))
                {
                    failures.Add($"{displayName}, line {body + 1} : {text}");
                }
            }
        }

        return failures;
    }

    private static void AssertSnippetCheckerWorks()
    {
        var root = RepositoryPaths.Root;
        const string Sample = "samples/CmdStarter.Samples.HelloWorld/Program.cs";
        var realLine = File.ReadAllLines(Path.Combine(root, Sample.Replace('/', Path.DirectorySeparatorChar))).Select(line => line.Trim()).First(line => line.Length > 0);
        var marker = $"<!-- snippet: {Sample} -->";
        Assert.Multiple(() =>
        {
            Assert.That(FindSnippetProblems("ok.md", [marker, string.Empty, "```csharp", realLine, "// ...", string.Empty, "```"], root), Is.Empty);
            Assert.That(FindSnippetProblems("invented.md", [marker, "```csharp", realLine, "this line is not in the file", "```"], root), Has.Count.EqualTo(1));
            Assert.That(FindSnippetProblems("nomarker.md", ["text", "```csharp", realLine, "```"], root), Has.Count.EqualTo(1));
            Assert.That(FindSnippetProblems("nofile.md", ["<!-- snippet: samples/NoSuch/Program.cs -->", "```csharp", realLine, "```"], root), Has.Count.EqualTo(1));
            Assert.That(FindSnippetProblems("other.md", ["```text", "invented", "```", "```powershell", "also invented", "```"], root), Is.Empty);
        });
    }

    // ---- Identifiers ----

    private static List<string> FindIdentifierProblems(string displayName, string[] lines)
    {
        var failures = new List<string>();
        foreach (var (number, text) in ProseLines(lines))
        {
            foreach (Match match in InlineCodeToken.Matches(text))
            {
                var token = match.Groups[1].Value.Trim();
                var problem = CheckIdentifier(token);
                if (problem is not null)
                {
                    failures.Add($"{displayName}, line {number} : {token} ({problem})");
                }
            }
        }

        return failures;
    }

    // Returns the reason why a code token names something that does not exist, or null when it exists or is not a token that is checked:
    // "Type.Member[.Member...]" whose first segment is a public type of the library, or "[Attribute]" / "[Attribute(...)]" / "[Attribute<T>]".
    private static string? CheckIdentifier(string token)
    {
        var attribute = AttributeToken.Match(token);
        if (attribute.Success)
        {
            var name = attribute.Groups["name"].Value;
            return AttributeNames.Value.Contains(name) ? null : $"no attribute type named {name} or {name}Attribute";
        }

        var segments = SplitDottedName(token);
        if (segments is null || segments.Count < 2)
        {
            return null;
        }

        var current = LibraryTypes.Value.Where(type => WithoutArity(type.Name) == segments[0]).ToList();
        if (current.Count == 0)
        {
            return null;
        }

        for (var index = 1; index < segments.Count; index++)
        {
            var next = new List<Type>();
            foreach (var type in current)
            {
                if (type.IsGenericParameter)
                {
                    return null;
                }

                foreach (var member in FindMembers(type, segments[index]))
                {
                    next.AddRange(MemberTypes(member));
                }
            }

            if (!current.Any(type => FindMembers(type, segments[index]).Any()))
            {
                return $"{string.Join('.', segments.Take(index))} has no public member '{segments[index]}'";
            }

            current = next;
        }

        return null;
    }

    // Splits "A.B<T>(x, y).C" on its dots, dropping the generic arguments and the call arguments of each segment.
    // Returns null when the token has another shape (spaces, commands, text...), which is then not checked.
    private static List<string>? SplitDottedName(string token)
    {
        var segments = new List<string>();
        var position = 0;
        while (true)
        {
            var start = position;
            if (position >= token.Length || !(char.IsLetter(token[position]) || token[position] == '_'))
            {
                return null;
            }

            while (position < token.Length && (char.IsLetterOrDigit(token[position]) || token[position] == '_'))
            {
                position++;
            }

            segments.Add(token[start..position]);
            if (position < token.Length && token[position] == '<' && !SkipBalanced(token, ref position, '<', '>'))
            {
                return null;
            }

            if (position < token.Length && token[position] == '(' && !SkipBalanced(token, ref position, '(', ')'))
            {
                return null;
            }

            if (position == token.Length)
            {
                return segments;
            }

            if (token[position] != '.')
            {
                return null;
            }

            position++;
        }
    }

    private static bool SkipBalanced(string text, ref int position, char open, char close)
    {
        var depth = 0;
        for (; position < text.Length; position++)
        {
            if (text[position] == open)
            {
                depth++;
            }
            else if (text[position] == close && --depth == 0)
            {
                position++;
                return true;
            }
        }

        return false;
    }

    // The public members of a type, inherited ones and the ones of its interfaces included (a nested type is a member).
    private static IEnumerable<MemberInfo> FindMembers(Type type, string name)
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        return type.GetMembers(Flags)
            .Concat(type.GetInterfaces().SelectMany(contract => contract.GetMembers(Flags)))
            .Where(member => member.Name == name);
    }

    private static IEnumerable<Type> MemberTypes(MemberInfo member) => member switch
    {
        PropertyInfo property => [property.PropertyType],
        FieldInfo field => [field.FieldType],
        MethodInfo method => [method.ReturnType],
        EventInfo @event when @event.EventHandlerType is not null => [@event.EventHandlerType],
        Type nested => [nested],
        _ => []
    };

    private static string WithoutArity(string typeName)
    {
        var tick = typeName.IndexOf('`', StringComparison.Ordinal);
        return tick >= 0 ? typeName[..tick] : typeName;
    }

    private static Assembly[] LibraryAssemblies() => [typeof(Starter).Assembly, typeof(CmdStarterHostExtensions).Assembly];

    // The names of the attribute types of the library and of every assembly loaded in the test process (so [Required], [Description], [Test] are known),
    // with and without the "Attribute" suffix.
    private static HashSet<string> FindAttributeNames()
    {
        // The .NET attributes that the documentation cites are searched too, whether or not the test process has loaded their assembly yet.
        Assembly[] cited = [typeof(System.ComponentModel.DescriptionAttribute).Assembly, typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly];

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in LibraryAssemblies().Concat(cited).Concat(AppDomain.CurrentDomain.GetAssemblies()).Distinct().SelectMany(ExportedTypes))
        {
            if (!typeof(Attribute).IsAssignableFrom(type))
            {
                continue;
            }

            var name = WithoutArity(type.Name);
            names.Add(name);
            if (name.EndsWith("Attribute", StringComparison.Ordinal))
            {
                names.Add(name[..^"Attribute".Length]);
            }
        }

        return names;
    }

    private static IEnumerable<Type> ExportedTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetExportedTypes();
        }
        catch (Exception exception) when (exception is NotSupportedException or FileNotFoundException or FileLoadException or ReflectionTypeLoadException)
        {
            return [];
        }
    }

    private static void AssertIdentifierCheckerWorks()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CheckIdentifier("Starter.Start"), Is.Null);
            Assert.That(CheckIdentifier("Starter.Start(args, console, token)"), Is.Null);
            Assert.That(CheckIdentifier("[AvailableInState(\"open\")]"), Is.Null);
            Assert.That(CheckIdentifier("[Parent<T>]"), Is.Null);
            Assert.That(CheckIdentifier("[Required]"), Is.Null);
            Assert.That(CheckIdentifier("ReplStarter.Namespaces"), Is.Null);
            Assert.That(CheckIdentifier("CommandSequenceStep.Pipeline.Count"), Is.Null);
            Assert.That(CheckIdentifier("SequenceOperator.OnSuccess"), Is.Null);
            Assert.That(CheckIdentifier("CmdStarterHostExtensions.RunCmdStarterAsync"), Is.Null);
            Assert.That(CheckIdentifier("Starter.NoSuchMember"), Is.Not.Null);
            Assert.That(CheckIdentifier("Starter.Start.NoSuchMember"), Is.Not.Null);
            Assert.That(CheckIdentifier("[NoSuchThing]"), Is.Not.Null);
            Assert.That(CheckIdentifier("System.Console"), Is.Null);
            Assert.That(CheckIdentifier("Starter"), Is.Null);
            Assert.That(CheckIdentifier("dotnet run --project samples/x -- demo"), Is.Null);
            Assert.That(CheckIdentifier("Starter.Start and more"), Is.Null);
        });
    }
}
