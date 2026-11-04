using System.Collections.Immutable;
using CyberInternauts.CmdStarter;
using CyberInternauts.CmdStarter.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CyberInternauts.CmdStarter.Analyzers.Tests;

internal static class AnalyzerRunner
{
    internal static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, bool includeCmdStarter = true)
    {
        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList();
        if (includeCmdStarter)
        {
            references.Add(MetadataReference.CreateFromFile(typeof(Starter).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(System.CommandLine.Command).Assembly.Location));
        }

        var compilation = CSharpCompilation.Create(
            "AnalyzerInput",
            new[] { CSharpSyntaxTree.ParseText("#nullable enable\n" + source, new CSharpParseOptions(LanguageVersion.Preview)) },
            references.DistinctBy(reference => reference.Display),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        }

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new ParentAttributeAnalyzer(), new AutoCompleteAttributeAnalyzer(), new CommandDefinitionAnalyzer(), new OptionsAnalyzer());
        var diagnostics = await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();
        return diagnostics.OrderBy(diagnostic => diagnostic.Location.SourceSpan.Start).ToImmutableArray();
    }
}
