using System.Collections.Immutable;
using CyberInternauts.CmdStarter;
using CyberInternauts.CmdStarter.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CyberInternauts.CmdStarter.Analyzers.Tests;

internal static class AnalyzerRunner
{
    // The assemblies of CmdStarter are among the platform assemblies of the test process, because the test project references them:
    // a source "without CmdStarter" has to leave them out.
    private static readonly string[] CmdStarterAssemblies = { "CmdStarter.Lib.dll", "CmdStarter.Hosting.dll" };

    /// <param name="includeCmdStarter">When false, the source is compiled without any reference to CmdStarter.Lib.</param>
    /// <param name="allowCompilationErrors">When true, the analyzers run on a source that does not compile, as they do in an editor while code is being typed.</param>
    internal static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, bool includeCmdStarter = true, IReadOnlyDictionary<string, ReportDiagnostic>? severities = null, bool allowCompilationErrors = false)
    {
        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator)
            .Where(path => includeCmdStarter || !CmdStarterAssemblies.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
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
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithSpecificDiagnosticOptions(severities ?? new Dictionary<string, ReportDiagnostic>()));
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0 && !allowCompilationErrors)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        }

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new ParentAttributeAnalyzer(), new AutoCompleteAttributeAnalyzer(), new CommandDefinitionAnalyzer(), new OptionsAnalyzer());
        var diagnostics = await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();
        return diagnostics.OrderBy(diagnostic => diagnostic.Location.SourceSpan.Start).ToImmutableArray();
    }
}
