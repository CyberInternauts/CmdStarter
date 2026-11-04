using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CyberInternauts.CmdStarter.Analyzers.Tests;

/// <summary>What a run of the generator gave.</summary>
/// <param name="Generated">The text of the generated file, or null when no file was generated.</param>
/// <param name="GeneratorDiagnostics">The diagnostics that the generator reported (CMDS0014).</param>
/// <param name="Errors">The errors of the compilation that includes the generated file.</param>
/// <param name="GeneratedWarnings">The warnings of that compilation that come from the generated file: with TreatWarningsAsErrors, an application would fail on them.</param>
internal sealed record GeneratorResult(string? Generated, ImmutableArray<Diagnostic> GeneratorDiagnostics, ImmutableArray<Diagnostic> Errors, ImmutableArray<Diagnostic> GeneratedWarnings);

internal static class GeneratorRunner
{
    /// <param name="source">The source of the compilation.</param>
    /// <param name="assemblyName">The name of the assembly that is compiled.</param>
    /// <param name="includeCmdStarter">When false, the source is compiled without any reference to CmdStarter.Lib.</param>
    /// <param name="nullable">Whether the nullable context of the source is enabled, as <c>Nullable</c> in a project.</param>
    internal static GeneratorResult Run(string source, string? assemblyName = "GeneratorInput", bool includeCmdStarter = true, bool nullable = true)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { CSharpSyntaxTree.ParseText(source, parseOptions) },
            AnalyzerRunner.CreateReferences(includeCmdStarter),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: nullable ? NullableContextOptions.Enable : NullableContextOptions.Disable, allowUnsafe: true));

        // The same language version for the generated file as for the source: a compilation takes one.
        var driver = CSharpGeneratorDriver.Create(new[] { new CommandCatalogGenerator().AsSourceGenerator() }, parseOptions: parseOptions)
            .RunGeneratorsAndUpdateCompilation(compilation, out var updated, out _);
        var result = driver.GetRunResult();
        var generatedTree = result.GeneratedTrees.SingleOrDefault();

        var diagnostics = updated.GetDiagnostics();
        return new GeneratorResult(
            generatedTree?.GetText().ToString(),
            result.Diagnostics,
            [.. diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)],
            [.. diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning && generatedTree is not null && diagnostic.Location.SourceTree == generatedTree)]);
    }
}
