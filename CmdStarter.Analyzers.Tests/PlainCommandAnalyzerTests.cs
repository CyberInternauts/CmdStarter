using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace CyberInternauts.CmdStarter.Analyzers.Tests;

/// <summary>The rules about plain commands: a class marked [Command] without a [Command] method, whose handler is its only public instance method or the one that Handler names.</summary>
[TestFixture]
[Category("Analyzers")]
public sealed class PlainCommandAnalyzerTests
{
    private const string Usings = "using System;\nusing CyberInternauts.CmdStarter;\nusing CyberInternauts.CmdStarter.Attributes;\nusing CyberInternauts.CmdStarter.Interfaces;\n";

    [Test]
    public async Task ClassWithoutAnyMethodIsReported() => await AssertCount("CMDS0011", 1, "[Command] public sealed class C { }");

    [Test]
    public async Task ClassWithTwoPublicMethodsIsReported() => await AssertCount("CMDS0011", 1, "[Command] public sealed class C { public void A() { } public void B() { } }");

    [Test]
    public async Task OpenGenericClassIsReported() => await AssertCount("CMDS0011", 1, "[Command] public sealed class C<T> { public void Run() { } }");

    [Test]
    public async Task ClassWithExactlyOnePublicMethodHasNoDiagnostic() => await AssertNone("[Command] public sealed class C { public int Run() => 0; private void Helper() { } public static void Util() { } public string Name { get; set; } = \"\"; }");

    [Test]
    public async Task ObjectOverridesAndDisposeAreNotHandlers() => await AssertNone("""
        [Command] public sealed class C : IDisposable
        {
            public int Run() => 0;
            public override string ToString() => "";
            public override bool Equals(object? obj) => false;
            public override int GetHashCode() => 0;
            public void Dispose() { }
        }
        """);

    [Test]
    public async Task AsyncDisposeIsNotAHandler() => await AssertNone("[Command] public sealed class C : IAsyncDisposable { public int Run() => 0; public System.Threading.Tasks.ValueTask DisposeAsync() => default; }");

    [Test]
    public async Task ClassOfBothModelsIsReported() => await AssertCount("CMDS0009", 1, "[Command] public sealed class C : StarterCommand { }");

    [Test]
    public async Task GenericHandlerIsReported()
    {
        await AssertCount("CMDS0007", 1, "[Command] public sealed class C { public void Run<T>() { } }");
        await AssertCount("CMDS0007", 1, "[Command(Handler = nameof(Run))] public sealed class C { public void Run<T>() { } public void Other() { } }");
    }

    [Test]
    public async Task ByRefParameterOfTheHandlerIsReported()
    {
        await AssertCount("CMDS0008", 1, "[Command] public sealed class C { public void Run(ref int x) { } }");
        await AssertCount("CMDS0008", 2, "[Command(Handler = nameof(Run))] public sealed class C { public void Run(out int x, in int y) { x = 0; } public void Other() { } }");
    }

    [Test]
    public async Task HandlerThatDoesNotExistIsReported() => await AssertCount("CMDS0017", 1, "[Command(Handler = \"Missing\")] public sealed class C { public void Run() { } }");

    [Test]
    public async Task HandlerThatIsNotPublicIsReported() => await AssertCount("CMDS0017", 1, "[Command(Handler = nameof(Run))] public sealed class C { private void Run() { } public void Other() { } }");

    [Test]
    public async Task HandlerThatIsStaticIsReported() => await AssertCount("CMDS0017", 1, "[Command(Handler = nameof(Run))] public sealed class C { public static void Run() { } public void Other() { } }");

    [Test]
    public async Task HandlerThatNamesSeveralMethodsIsReported() => await AssertCount("CMDS0017", 1, "[Command(Handler = nameof(Run))] public sealed class C { public void Run() { } public void Run(int x) { } }");

    [Test]
    public async Task HandlerOnAMethodIsReported() => await AssertCount("CMDS0017", 1, "public sealed class C { [Command(Handler = \"x\")] public static void Run() { } }");

    [Test]
    public async Task HandlerOnAGroupIsReported() => await AssertCount("CMDS0017", 1, "[Command(\"group\", Handler = \"x\")] public sealed class C { [Command] public static void Run() { } }");

    [Test]
    public async Task HandlerOnAClassThatCannotBeAPlainCommandIsReported() => await AssertCount("CMDS0017", 1, "[Command(Handler = \"x\")] public abstract class C { public void Run() { } }");

    [Test]
    public async Task HandlerThatIsInheritedHasNoDiagnostic() => await AssertNone("""
        public class Base { public int Run() => 0; }
        [Command(Handler = nameof(Run))] public sealed class C : Base { public void Other() { } public void More() { } }
        """);

    [Test]
    public async Task HandlerThatOverridesAVirtualMethodIsOneMethod() => await AssertNone("""
        public class Base { public virtual int Run() => 0; }
        [Command(Handler = nameof(Run))] public sealed class C : Base { public override int Run() => 1; }
        """);

    [Test]
    public async Task OptionsOfAPlainCommandHideGlobalOptions()
    {
        var diagnostics = await AnalyzerRunner.GetDiagnosticsAsync(Usings + """
            public sealed class Global : IGlobalOptionsContainer { public bool Verbose { get; set; } public string Region { get; set; } = ""; }
            public class Base { public string Region { get; set; } = ""; }
            [Command] public sealed class C : Base
            {
                public bool Verbose { get; set; }
                public int Run([Option] bool verbose) => 0;
            }
            """);

        // The property of the class, the property of its base class, and the parameter of the handler.
        Assert.That(diagnostics.Count(diagnostic => diagnostic.Id == "CMDS0012"), Is.EqualTo(3));
        Assert.That(diagnostics, Has.All.Matches<Diagnostic>(diagnostic => diagnostic.Id == "CMDS0012"));
    }

    [Test]
    public async Task NotOptionAndAllOptionsExcludedPropertiesOfAPlainCommandHideNothing() => await AssertNone("""
        public sealed class Global : IGlobalOptionsContainer { public bool Verbose { get; set; } public string Region { get; set; } = ""; }
        [AllOptionsExcluded] public class Base { public string Region { get; set; } = ""; }
        [Command] public sealed class C : Base
        {
            [NotOption] public bool Verbose { get; set; }
            public int Run() => 0;
        }
        """);

    private static async Task AssertCount(string id, int count, string source)
    {
        var diagnostics = await AnalyzerRunner.GetDiagnosticsAsync(Usings + source);
        Assert.That(diagnostics.Count(diagnostic => diagnostic.Id == id), Is.EqualTo(count));
        Assert.That(diagnostics, Has.All.Matches<Diagnostic>(diagnostic => diagnostic.Id == id && diagnostic.Severity == DiagnosticSeverity.Warning));
    }

    private static async Task AssertNone(string source) => Assert.That((await AnalyzerRunner.GetDiagnosticsAsync(Usings + source)).IsEmpty);
}
