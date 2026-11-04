using CyberInternauts.CmdStarter.Extensions;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace CyberInternauts.CmdStarter.Analyzers.Tests;

/// <summary>
/// The paths of the rules that the main tests do not take: libraries that are only partly there, symbols found in namespaces and nested types,
/// aliases, and code that does not compile.
/// </summary>
[TestFixture]
[Category("Analyzers")]
public sealed class AnalyzerBranchTests
{
    private const string Attributes = "CyberInternauts.CmdStarter.Attributes";
    private const string Interfaces = "CyberInternauts.CmdStarter.Interfaces";

    [Test]
    public async Task EveryAnalyzerDoesNothingWhenOnlySomeCmdStarterTypesExist()
    {
        // Each analyzer looks for its types one after the other and stops at the first that is missing: for each analyzer,
        // the source declares the first k of its types, for every k, so that each type in turn is the one that is missing.
        var sequences = new[]
        {
            // OptionsAnalyzer
            new[]
            {
                Declare(Interfaces, "public interface IStarterCommand { }"),
                Declare(Interfaces, "public interface IGlobalOptionsContainer { }"),
                Declare(Attributes, "public sealed class OptionAttribute : System.Attribute { }"),
                Declare(Attributes, "public sealed class NotOptionAttribute : System.Attribute { }"),
                Declare(Attributes, "public sealed class AllOptionsExcludedAttribute : System.Attribute { }"),
                Declare(Attributes, "public sealed class AliasAttribute : System.Attribute { }")
            },
            // CommandDefinitionAnalyzer
            new[]
            {
                Declare(Attributes, "public sealed class CommandAttribute : System.Attribute { }"),
                Declare("CyberInternauts.CmdStarter", "public class StarterCommand { }"),
                Declare(Interfaces, "public interface IStarterCommand { }")
            },
            // AutoCompleteAttributeAnalyzer
            new[]
            {
                Declare(Attributes, "public class AutoCompleteAttribute : System.Attribute { }"),
                Declare(Attributes, "public sealed class AutoCompleteAttribute<T> : System.Attribute { }"),
                Declare(Interfaces, "public interface IAutoCompleteProvider { }"),
                Declare(Interfaces, "public interface IAutoCompleteFactory { }")
            },
            // ParentAttributeAnalyzer
            new[]
            {
                Declare(Attributes, "public class ParentAttribute : System.Attribute { }"),
                Declare(Attributes, "public class ParentAttribute<T> : ParentAttribute { }")
            }
        };

        foreach (var sequence in sequences)
        {
            for (var present = 0; present < sequence.Length; present++)
            {
                var source = string.Join(Environment.NewLine, sequence.Take(present));
                var diagnostics = await AnalyzerRunner.GetDiagnosticsAsync(source, includeCmdStarter: false);

                Assert.That(diagnostics, Is.Empty, $"{present} type(s) declared: {source}");
            }
        }
    }

    [Test]
    public async Task GlobalOptionsInNamespacesAndNestedTypesHideCommandOptions() => await AssertCount("CMDS0012", 3, """
        using CyberInternauts.CmdStarter;
        using CyberInternauts.CmdStarter.Interfaces;
        namespace Outer.Inner { public sealed class DeepGlobal : IGlobalOptionsContainer { public bool Deep { get; set; } } }
        public sealed class Holder
        {
            public sealed class NestedGlobal : IGlobalOptionsContainer
            {
                public bool Nested { get; set; }
                public sealed class DeeperGlobal : IGlobalOptionsContainer { public bool Deeper { get; set; } }
            }
        }
        public sealed class C : StarterCommand { public bool Deep { get; set; } public bool Nested { get; set; } public bool Deeper { get; set; } public bool Fine { get; set; } }
        """);

    [Test]
    public async Task AliasesOfOptionsHideGlobalOptionsToo() => await AssertCount("CMDS0012", 3, """
        using CyberInternauts.CmdStarter;
        using CyberInternauts.CmdStarter.Attributes;
        using CyberInternauts.CmdStarter.Interfaces;
        public sealed class Global : IGlobalOptionsContainer
        {
            [Alias(true, "q")] public bool Quiet { get; set; }
            [Alias("--out", "-o")] public string Output { get; set; } = "";
        }
        public sealed class WithPrefixedAlias : StarterCommand { [Alias(true, "q")] public bool First { get; set; } }
        public sealed class WithPlainAlias : StarterCommand { [Alias("-o")] public bool Second { get; set; } }
        public sealed class WithOwnPrefix : StarterCommand { [Alias(true, "output", Prefix = "--")] public bool Third { get; set; } }
        public sealed class WithoutPrefix : StarterCommand { [Alias(false, "q")] public bool Fourth { get; set; } }
        public sealed class Unrelated : StarterCommand { [Alias("unrelated")] public bool Fifth { get; set; } }
        """);

    [Test]
    public async Task PropertyWithoutOptionAttributeIsNotAnOptionWhenAnotherPropertyHasIt()
    {
        const string Types = """
            using CyberInternauts.CmdStarter;
            using CyberInternauts.CmdStarter.Attributes;
            using CyberInternauts.CmdStarter.Interfaces;
            public sealed class Global : IGlobalOptionsContainer { public bool Verbose { get; set; } }
            """;

        // [Option] on one property makes the other properties plain properties: Verbose is not an option, so nothing is hidden.
        Assert.That(await AnalyzerRunner.GetDiagnosticsAsync(Types + """
            public sealed class C : StarterCommand { [Option] public bool Other { get; set; } public bool Verbose { get; set; } }
            """), Is.Empty);

        // Without any [Option], every public property is an option.
        await AssertCount("CMDS0012", 1, Types + """
            public sealed class C : StarterCommand { public bool Other { get; set; } public bool Verbose { get; set; } }
            """);
    }

    [Test]
    public async Task NotOptionPropertyIsNotAnOption() => Assert.That(await AnalyzerRunner.GetDiagnosticsAsync("""
        using CyberInternauts.CmdStarter;
        using CyberInternauts.CmdStarter.Attributes;
        using CyberInternauts.CmdStarter.Interfaces;
        public sealed class Global : IGlobalOptionsContainer { public bool Verbose { get; set; } }
        public sealed class C : StarterCommand { [NotOption] public bool Verbose { get; set; } }
        """), Is.Empty);

    [Test]
    public async Task BaseInitializersThatAreNotAStarterCommandNameAreIgnored() => Assert.That(await AnalyzerRunner.GetDiagnosticsAsync("""
        using CyberInternauts.CmdStarter;
        public class Plain { public Plain(string text) { } }
        public sealed class OtherBase : Plain { public OtherBase() : base("") { } }
        public sealed class Chained : StarterCommand { public Chained() : this(1) { } public Chained(int value) : base("ok") { } }
        public sealed class NoArgument : StarterCommand { public NoArgument() : base() { } }
        public sealed class Computed : StarterCommand { public Computed() : base(Name()) { } private static string Name() => ""; }
        """), Is.Empty);

    [Test]
    public async Task NullNameGivenToStarterCommandIsReported() => await AssertCount("CMDS0005", 1, """
        using CyberInternauts.CmdStarter;
        public sealed class C : StarterCommand { public C() : base((string?)null!) { } }
        """);

    [Test]
    public async Task AnalyzersDoNotFailOnCodeThatDoesNotCompile()
    {
        var diagnostics = await AnalyzerRunner.GetDiagnosticsAsync("""
            using CyberInternauts.CmdStarter;
            using CyberInternauts.CmdStarter.Attributes;
            public sealed class C : StarterCommand
            {
                public C() : base(undefined) { }
                [Unknown] public string P { get; set; } = "";
                [AutoComplete(typeof(string))] public string Q { get; set; } = "";
                [Alias(undefined)] public bool R { get; set; }
            }
            public static class M
            {
                [Command] public static void A() { }
                [Command(undefinedName)] public static void B() { }
                [Command(5)] public static void D() { }
                [Command()] public static void E() { }
                public static string NonConstant = "x";
                [Command(NonConstant)] public static void F() { }
                [Command(null!)] public static void G() { }
            }
            """, allowCompilationErrors: true);

        // The analyzers go on after the symbols they cannot read, and none of them fails.
        Assert.That(diagnostics.Where(diagnostic => diagnostic.Id == "AD0001"), Is.Empty);
        Assert.That(diagnostics.Count(diagnostic => diagnostic.Id == "CMDS0004"), Is.EqualTo(1));
    }

    [Test]
    public async Task AutoCompleteWithTypeThatIsNotANamedTypeIsUnsupported() => await AssertCount("CMDS0004", 1, """
        using CyberInternauts.CmdStarter;
        using CyberInternauts.CmdStarter.Attributes;
        public sealed class C : StarterCommand { [AutoComplete(typeof(int[]))] public string A { get; set; } = ""; }
        """);

    [Test]
    public async Task NullAliasArrayNamesNoAlias() => Assert.That(await AnalyzerRunner.GetDiagnosticsAsync("""
        using CyberInternauts.CmdStarter;
        using CyberInternauts.CmdStarter.Attributes;
        using CyberInternauts.CmdStarter.Interfaces;
        public sealed class Global : IGlobalOptionsContainer { public bool Verbose { get; set; } }
        public sealed class WithPlainAlias : StarterCommand { [Alias((string[]?)null!)] public bool First { get; set; } }
        public sealed class WithPrefixedAlias : StarterCommand { [Alias(true, (string[]?)null!)] public bool Second { get; set; } }
        """), Is.Empty, "a null array names no alias: nothing is hidden and no analyzer fails");

    [Test]
    public async Task AutoCompleteWithNullArrayHasNoCompletion()
    {
        var diagnostics = await AnalyzerRunner.GetDiagnosticsAsync("""
            using CyberInternauts.CmdStarter;
            using CyberInternauts.CmdStarter.Attributes;
            public sealed class C : StarterCommand { [AutoComplete((object[]?)null!)] public string A { get; set; } = ""; }
            """);

        Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Is.EqualTo(new[] { "CMDS0003" }), "no text and no type: the completion is empty, and the analyzer does not fail");
    }

    [Test]
    public async Task AutoCompleteWithOnlyNullAndBlankValuesIsEmpty() => await AssertCount("CMDS0003", 1, """
        using CyberInternauts.CmdStarter;
        using CyberInternauts.CmdStarter.Attributes;
        public sealed class C : StarterCommand { [AutoComplete(null, " ")] public string A { get; set; } = ""; }
        """);

    [Test]
    public async Task ParentCycleBesideATypeIsReportedForItsMembersOnly() => await AssertCount("CMDS0001", 2, """
        using CyberInternauts.CmdStarter;
        using CyberInternauts.CmdStarter.Attributes;
        [Parent<B>] public sealed class A : StarterCommand { }
        [Parent<C>] public sealed class B : StarterCommand { }
        [Parent<B>] public sealed class C : StarterCommand { }
        """);

    [Test]
    public async Task ParentAttributeSubclassWithoutArgumentHasNoParent() => Assert.That(await AnalyzerRunner.GetDiagnosticsAsync("""
        using CyberInternauts.CmdStarter;
        using CyberInternauts.CmdStarter.Attributes;
        public sealed class NoParentAttribute : ParentAttribute { public NoParentAttribute() : base(null) { } }
        [NoParent] public sealed class A : StarterCommand { }
        """), Is.Empty);

    [Test]
    public void KebabCaseHandlesEmptyNullAndSeveralDigits()
    {
        Assert.That(NameConversion.PascalToKebabCase(null), Is.Empty);
        Assert.That(NameConversion.PascalToKebabCase(string.Empty), Is.Empty);
        foreach (var value in new[] { "Port80", "Ab123Cd", "A12", "V2Rate99" })
        {
            Assert.That(NameConversion.PascalToKebabCase(value), Is.EqualTo(value.PascalToKebabCase()), value);
        }

        Assert.That(string.Empty.PascalToKebabCase(), Is.Empty, "the library gives the same result for an empty name");
    }

    private static string Declare(string ns, string declaration) => $"namespace {ns} {{ {declaration} }}";

    private static async Task AssertCount(string id, int count, string source)
    {
        var diagnostics = await AnalyzerRunner.GetDiagnosticsAsync(source);
        Assert.That(diagnostics.Count(diagnostic => diagnostic.Id == id), Is.EqualTo(count));
        Assert.That(diagnostics, Has.All.Matches<Diagnostic>(diagnostic => diagnostic.Id == id));
    }
}
