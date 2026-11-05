using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CyberInternauts.CmdStarter.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParentAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptors.ParentCycle, Descriptors.MultipleParents];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var parent = start.Compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Attributes.ParentAttribute");
            var genericParent = start.Compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Attributes.ParentAttribute`1");
            if (parent is null || genericParent is null)
            {
                return;
            }

            start.RegisterSymbolAction(action => AnalyzeType(action, parent, genericParent), SymbolKind.NamedType);
        });
    }

    private static void AnalyzeType(SymbolAnalysisContext context, INamedTypeSymbol parent, INamedTypeSymbol genericParent)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        var attrs = type.GetAttributes().Where(attribute => IsParent(attribute.AttributeClass, parent, genericParent)).ToArray();
        if (attrs.Length > 1)
        {
            foreach (var attribute in attrs.Skip(1))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.MultipleParents, attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? type.Locations.FirstOrDefault(), type.Name));
            }
        }

        if (attrs.Length == 0)
        {
            return;
        }

        var firstLocation = attrs[0].ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? type.Locations.FirstOrDefault();
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var current = type;
        while (seen.Add(current))
        {
            var parentAttribute = current.GetAttributes().FirstOrDefault(attribute => IsParent(attribute.AttributeClass, parent, genericParent));
            if (parentAttribute is null || GetParentType(parentAttribute, genericParent) is not { } next)
            {
                break;
            }

            if (SymbolEqualityComparer.Default.Equals(next, type))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.ParentCycle, firstLocation, type.Name));
                break;
            }

            current = next;
        }
    }

    private static INamedTypeSymbol? GetParentType(AttributeData attribute, INamedTypeSymbol genericParent)
    {
        if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass!.OriginalDefinition, genericParent))
        {
            return attribute.AttributeClass.TypeArguments[0] as INamedTypeSymbol;
        }

        return attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as INamedTypeSymbol : null;
    }

    private static bool IsParent(INamedTypeSymbol? candidate, INamedTypeSymbol parent, INamedTypeSymbol genericParent)
    {
        for (var current = candidate; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, parent) || SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, genericParent))
            {
                return true;
            }
        }

        return false;
    }
}
