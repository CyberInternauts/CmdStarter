using CyberInternauts.CmdStarter.Analyzers;
using CyberInternauts.CmdStarter.Extensions;
using NUnit.Framework;

namespace CyberInternauts.CmdStarter.Analyzers.Tests;

[TestFixture]
[Category("Analyzers")]
public sealed class AnalyzerTests
{
    [Test]
    public async Task ParentCycleIsReported() => await AssertCount("CMDS0001", 2, """
        using CyberInternauts.CmdStarter;
        using CyberInternauts.CmdStarter.Attributes;
        [Parent<B>] public sealed class A : StarterCommand { }
        [Parent<A>] public sealed class B : StarterCommand { }
        """);

    [Test]
    public async Task TwoParentAttributesAreReported() => await AssertCount("CMDS0002", 1, """
        using CyberInternauts.CmdStarter;
        using CyberInternauts.CmdStarter.Attributes;
        [Parent<B>, Parent(typeof(C))] public sealed class A : StarterCommand { }
        public sealed class B : StarterCommand { }
        public sealed class C : StarterCommand { }
        """);

    [Test]
    public async Task AutoCompleteWithoutCompletionIsReported() => await AssertCount("CMDS0003", 2, """
        using CyberInternauts.CmdStarter;
        using CyberInternauts.CmdStarter.Attributes;
        using CyberInternauts.CmdStarter.Interfaces;
        public sealed class OnlyFactory : IAutoCompleteFactory { public static IAutoCompleteFactory GetInstance() => new OnlyFactory(); }
        public sealed class C : StarterCommand { [AutoComplete("", " ")] public string A { get; set; } = ""; [AutoComplete(typeof(OnlyFactory))] public string B { get; set; } = ""; }
        """);

    [Test]
    public async Task UnsupportedAutoCompleteTypeIsReported() => await AssertCount("CMDS0004", 2, """
        using CyberInternauts.CmdStarter;
        using CyberInternauts.CmdStarter.Attributes;
        public sealed class C : StarterCommand { [AutoComplete(typeof(string))] public string A { get; set; } = ""; [AutoComplete<int>] public string B { get; set; } = ""; }
        """);

    [Test]
    public async Task BlankCommandNameIsReported() => await AssertCount("CMDS0005", 2, """
        using CyberInternauts.CmdStarter;
        using CyberInternauts.CmdStarter.Attributes;
        [Command(" ")] public static class Group { [Command] public static void Run() { } }
        public sealed class C : StarterCommand { public C() : base("") { } }
        """);

    [Test]
    public async Task NonPublicCommandMethodIsReported() => await AssertCount("CMDS0006", 1, """
        using CyberInternauts.CmdStarter.Attributes;
        public static class C { [Command] private static void Run() { } }
        """);

    [Test]
    public async Task GenericCommandMethodIsReported() => await AssertCount("CMDS0007", 1, """
        using CyberInternauts.CmdStarter.Attributes;
        public static class C { [Command] public static void Run<T>() { } }
        """);

    [Test]
    public async Task ByRefParameterIsReported() => await AssertCount("CMDS0008", 3, """
        using CyberInternauts.CmdStarter.Attributes;
        public static class C { [Command] public static void A(ref int x) { } [Command] public static void B(out int x) { x = 0; } [Command] public static void D(in int x) { } }
        """);

    [Test]
    public async Task MixedCommandModelsIsReported() => await AssertCount("CMDS0009", 1, """
        using CyberInternauts.CmdStarter;
        using CyberInternauts.CmdStarter.Attributes;
        public sealed class C : StarterCommand { [Command] public static void Run() { } }
        """);

    [Test]
    public async Task AbstractInstanceCommandIsReported() => await AssertCount("CMDS0010", 1, """
        using CyberInternauts.CmdStarter.Attributes;
        public abstract class C { [Command] public void Run() { } }
        """);

    [Test]
    public async Task EmptyCommandGroupIsReported() => await AssertCount("CMDS0011", 1, """
        using CyberInternauts.CmdStarter.Attributes;
        [Command("admin")] public static class C { }
        """);

    [Test]
    public async Task ValidDefinitionsHaveNoDiagnostic() => Assert.That((await AnalyzerRunner.GetDiagnosticsAsync("""
        using System;
        using System.Threading;
        using CyberInternauts.CmdStarter;
        using CyberInternauts.CmdStarter.Attributes;
        using CyberInternauts.CmdStarter.Interfaces;
        [Parent(null)] public sealed class Parentless : StarterCommand { }
        public enum SomeEnum { One }
        public sealed class SomeProvider : IAutoCompleteProvider { public static IAutoCompleteProvider GetInstance() => new SomeProvider(); public string[] GetAutoCompletes() => new[] { "a" }; }
        public sealed class Factory : IAutoCompleteFactory { public static IAutoCompleteFactory GetInstance() => new Factory(); }
        public sealed class C : StarterCommand { [AutoComplete("a", "b")] public string A { get; set; } = ""; [AutoComplete(typeof(SomeEnum))] public string B { get; set; } = ""; [AutoComplete<SomeProvider>] public string D { get; set; } = ""; [AutoComplete<Factory>("x")] public string E { get; set; } = ""; }
        public sealed class Global : IGlobalOptionsContainer { public string Region { get; set; } = ""; }
        [Command("admin")] public static class Group { [Command] public static void A() { } [Command] public static void B() { } }
        public static class StaticCommands { [Command] public static void Run(CancellationToken token) { } }
        public abstract class AbstractCommands { [Command] public static void Run() { } }
        public sealed class Named : StarterCommand { public Named() : base("ok") { } public int Count { get; set; } }
        """)).IsEmpty);

    [Test]
    public async Task NoCmdStarterReferenceDoesNothing() => Assert.That((await AnalyzerRunner.GetDiagnosticsAsync("public sealed class Plain { }", includeCmdStarter: false)).IsEmpty);

    [Test]
    public async Task OptionHidingGlobalOptionIsReported() => await AssertCount("CMDS0012", 2, """
        using CyberInternauts.CmdStarter;
        using CyberInternauts.CmdStarter.Attributes;
        using CyberInternauts.CmdStarter.Interfaces;
        public sealed class Global : IGlobalOptionsContainer { public bool Verbose { get; set; } }
        public sealed class C : StarterCommand { public bool Verbose { get; set; } }
        public static class M { [Command] public static void Run([Option] bool verbose) { } }
        """);

    [Test]
    public async Task EmptyGlobalOptionsContainerIsReported() => await AssertCount("CMDS0013", 2, """
        using CyberInternauts.CmdStarter.Attributes;
        using CyberInternauts.CmdStarter.Interfaces;
        public sealed class ReadOnly : IGlobalOptionsContainer { public string Value => ""; }
        [AllOptionsExcluded] public sealed class Excluded : IGlobalOptionsContainer { public int A { get; set; } public int B { get; set; } }
        public abstract class Abstract : IGlobalOptionsContainer { }
        """);

    [Test]
    public void KebabCaseMatchesLibrary()
    {
        foreach (var value in new[] { "Verbose", "MyOption", "HTTPPort", "Option2", "A1B2", "X", "lowercase", "ABC" })
        {
            Assert.That(NameConversion.PascalToKebabCase(value), Is.EqualTo(value.PascalToKebabCase()));
        }
    }

    private static async Task AssertCount(string id, int count, string source)
    {
        var diagnostics = await AnalyzerRunner.GetDiagnosticsAsync(source);
        Assert.That(diagnostics.Count(diagnostic => diagnostic.Id == id), Is.EqualTo(count));
        Assert.That(diagnostics, Has.All.Matches<Microsoft.CodeAnalysis.Diagnostic>(diagnostic => diagnostic.Id == id));
    }
}

