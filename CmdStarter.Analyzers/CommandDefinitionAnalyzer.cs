using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CyberInternauts.CmdStarter.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommandDefinitionAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        Descriptors.EmptyCommandName, Descriptors.NonPublicMethod, Descriptors.GenericMethod, Descriptors.ByRefParameter,
        Descriptors.MixedModels, Descriptors.AbstractInstanceMethod, Descriptors.EmptyGroup);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var command = start.Compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Attributes.CommandAttribute");
            var starterCommand = start.Compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.StarterCommand");
            var starterInterface = start.Compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Interfaces.IStarterCommand");
            if (command is null || starterCommand is null || starterInterface is null)
            {
                return;
            }

            start.RegisterSymbolAction(action => AnalyzeMethod(action, command), SymbolKind.Method);
            start.RegisterSymbolAction(action => AnalyzeType(action, command, starterCommand, starterInterface), SymbolKind.NamedType);
            start.RegisterSyntaxNodeAction(action => AnalyzeCommandAttribute(action, command), Microsoft.CodeAnalysis.CSharp.SyntaxKind.Attribute);
            start.RegisterSyntaxNodeAction(AnalyzeBaseConstructorDeclaration, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ConstructorDeclaration);
        });
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol commandAttribute)
    {
        var method = (IMethodSymbol)context.Symbol;
        var attribute = method.GetAttributes().FirstOrDefault(item => SymbolEqualityComparer.Default.Equals(item.AttributeClass, commandAttribute));
        if (attribute is null)
        {
            return;
        }

        var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? method.Locations.FirstOrDefault();
        if (method.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.NonPublicMethod, location, method.Name));
        }

        if (method.IsGenericMethod)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.GenericMethod, location, method.Name));
        }

        foreach (var parameter in method.Parameters.Where(parameter => parameter.RefKind != RefKind.None))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.ByRefParameter, parameter.Locations.FirstOrDefault(), parameter.Name));
        }

        if (method.ContainingType.TypeKind == TypeKind.Class && method.ContainingType.IsAbstract && !method.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.AbstractInstanceMethod, location, method.ContainingType.Name));
        }
    }

    private static void AnalyzeType(SymbolAnalysisContext context, INamedTypeSymbol commandAttribute, INamedTypeSymbol starterCommand, INamedTypeSymbol starterInterface)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        var methods = type.GetMembers().OfType<IMethodSymbol>().Where(method => method.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, commandAttribute))).ToArray();
        if (methods.Length > 0 && Implements(type, starterInterface))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.MixedModels, methods[0].GetAttributes().First(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, commandAttribute)).ApplicationSyntaxReference?.GetSyntax().GetLocation(), type.Name));
        }

        var classAttribute = type.GetAttributes().FirstOrDefault(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, commandAttribute));
        if (classAttribute is not null && methods.Length == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.EmptyGroup, classAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? type.Locations.FirstOrDefault(), type.Name));
        }
    }

    private static void AnalyzeCommandAttribute(SyntaxNodeAnalysisContext context, INamedTypeSymbol commandAttribute)
    {
        var syntax = (AttributeSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(syntax).Symbol is not IMethodSymbol constructor
            || !SymbolEqualityComparer.Default.Equals(constructor.ContainingType, commandAttribute)
            || syntax.ArgumentList?.Arguments.FirstOrDefault()?.Expression is not { } expression)
        {
            return;
        }

        var constant = context.SemanticModel.GetConstantValue(expression);
        if (constant.HasValue && constant.Value is string name && string.IsNullOrWhiteSpace(name))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.EmptyCommandName, expression.GetLocation()));
        }
    }

    private static void AnalyzeBaseConstructorDeclaration(SyntaxNodeAnalysisContext context)
    {
        var declaration = (ConstructorDeclarationSyntax)context.Node;
        var initializer = declaration.Initializer;
        if (initializer is null
            || !initializer.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.BaseConstructorInitializer)
            || initializer.ArgumentList.Arguments.Count == 0
            || context.SemanticModel.GetSymbolInfo(initializer).Symbol is not IMethodSymbol constructor
            || !StringComparer.Ordinal.Equals(constructor.ContainingType.ToDisplayString(), "CyberInternauts.CmdStarter.StarterCommand"))
        {
            return;
        }

        var expression = initializer.ArgumentList.Arguments[0].Expression;
        var constant = context.SemanticModel.GetConstantValue(expression);
        if (constant.HasValue && string.IsNullOrWhiteSpace(constant.Value as string))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.EmptyCommandName, expression.GetLocation()));
        }
    }

    private static bool Implements(INamedTypeSymbol type, INamedTypeSymbol iface) => type.AllInterfaces.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, iface));
}
