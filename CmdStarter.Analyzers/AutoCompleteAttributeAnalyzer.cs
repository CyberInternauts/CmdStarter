using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CyberInternauts.CmdStarter.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AutoCompleteAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptors.EmptyCompletion, Descriptors.UnsupportedCompletion);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var auto = start.Compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Attributes.AutoCompleteAttribute");
            var generic = start.Compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Attributes.AutoCompleteAttribute`1");
            var provider = start.Compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Interfaces.IAutoCompleteProvider");
            var factory = start.Compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Interfaces.IAutoCompleteFactory");
            if (auto is null || generic is null || provider is null || factory is null)
            {
                return;
            }

            start.RegisterSymbolAction(action => AnalyzeAttributes(action, auto, generic, provider, factory), SymbolKind.Property, SymbolKind.Parameter);
        });
    }

    private static void AnalyzeAttributes(SymbolAnalysisContext context, INamedTypeSymbol auto, INamedTypeSymbol generic, INamedTypeSymbol provider, INamedTypeSymbol factory)
    {
        ImmutableArray<AttributeData> attributes = context.Symbol.GetAttributes();
        foreach (var attribute in attributes)
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass is null || (!SymbolEqualityComparer.Default.Equals(attributeClass.OriginalDefinition, auto) && !SymbolEqualityComparer.Default.Equals(attributeClass.OriginalDefinition, generic)))
            {
                continue;
            }

            var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? context.Symbol.Locations.FirstOrDefault();
            var featureType = SymbolEqualityComparer.Default.Equals(attributeClass.OriginalDefinition, generic) ? attributeClass.TypeArguments[0] : null;
            var args = attribute.ConstructorArguments;
            if (featureType is null && args.Length > 0 && args[0].Kind == TypedConstantKind.Type)
            {
                featureType = args[0].Value as ITypeSymbol;
            }

            var supported = featureType is not null && (featureType.TypeKind == TypeKind.Enum || Implements(featureType, provider) || Implements(featureType, factory));
            if (featureType is not null && !supported)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.UnsupportedCompletion, location, featureType.ToDisplayString()));
                continue;
            }

            var hasText = args.Skip(featureType is not null && args.Length > 0 && args[0].Kind == TypedConstantKind.Type ? 1 : 0)
                .Any(argument => HasNonBlankValue(argument));
            var canSupply = featureType is not null && (featureType.TypeKind == TypeKind.Enum && ((INamedTypeSymbol)featureType).GetMembers().OfType<IFieldSymbol>().Any(field => field.HasConstantValue)
                || Implements(featureType, provider));
            if (!hasText && !canSupply)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.EmptyCompletion, location, context.Symbol.Name));
            }
        }
    }

    private static bool HasNonBlankValue(TypedConstant value)
    {
        if (value.Kind == TypedConstantKind.Array)
        {
            return value.Values.Any(HasNonBlankValue);
        }

        return value.Value is string text && !string.IsNullOrWhiteSpace(text);
    }

    private static bool Implements(ITypeSymbol type, INamedTypeSymbol iface) => type is INamedTypeSymbol named
        && named.AllInterfaces.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, iface));
}
