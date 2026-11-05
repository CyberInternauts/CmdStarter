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
        Descriptors.MixedModels, Descriptors.AbstractInstanceMethod, Descriptors.MissingHandler, Descriptors.InvalidHandler);

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

        ReportShape(context, method, location);

        if (PlainCommands.GetHandlerName(attribute) is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.InvalidHandler, HandlerLocation(attribute, location), method.Name, "Handler names the handler of a plain command: it cannot be used on a command method"));
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
        var classAttribute = type.GetAttributes().FirstOrDefault(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, commandAttribute));
        var classLocation = classAttribute?.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? type.Locations.FirstOrDefault();
        if (Implements(type, starterInterface))
        {
            // A class that implements IStarterCommand is a command of the class model: [Command] on it or on its methods makes it a command of the other model too.
            if (methods.Length > 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.MixedModels, methods[0].GetAttributes().First(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, commandAttribute)).ApplicationSyntaxReference?.GetSyntax().GetLocation(), type.Name));
            }
            else if (classAttribute is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.MixedModels, classLocation, type.Name));
            }

            return;
        }

        if (classAttribute is null)
        {
            return;
        }

        var handlerName = PlainCommands.GetHandlerName(classAttribute);
        if (methods.Length > 0)
        {
            // A group of command methods: the handler of a plain command has no meaning here.
            if (handlerName is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.InvalidHandler, HandlerLocation(classAttribute, classLocation), type.Name, "Handler names the handler of a plain command: it cannot be used on a class of command methods"));
            }

            return;
        }

        // [Command] without command methods: a plain command, if the class can be one.
        if (!PlainCommands.IsCandidate(type, commandAttribute, starterInterface))
        {
            context.ReportDiagnostic(handlerName is null
                ? Diagnostic.Create(Descriptors.MissingHandler, classLocation, type.Name)
                : Diagnostic.Create(Descriptors.InvalidHandler, HandlerLocation(classAttribute, classLocation), type.Name, "only a concrete class that is not generic can be a plain command"));
            return;
        }

        var handlers = PlainCommands.FindHandlers(type, handlerName, context.Compilation);
        if (handlers.Length == 1)
        {
            ReportShape(context, handlers[0], handlers[0].Locations.FirstOrDefault());
        }
        else if (handlerName is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.MissingHandler, classLocation, type.Name));
        }
        else
        {
            var reason = handlers.Length == 0
                ? "'" + handlerName + "' names no public instance method of the class (a method that is missing, not public or static does not count)"
                : "'" + handlerName + "' names several public instance methods";
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.InvalidHandler, HandlerLocation(classAttribute, classLocation), type.Name, reason));
        }
    }

    // A command method or the handler of a plain command cannot be generic and cannot have a ref, out or in parameter.
    private static void ReportShape(SymbolAnalysisContext context, IMethodSymbol method, Location? location)
    {
        if (method.IsGenericMethod)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.GenericMethod, location, method.Name));
        }

        foreach (var parameter in method.Parameters.Where(parameter => parameter.RefKind != RefKind.None))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.ByRefParameter, parameter.Locations.FirstOrDefault(), parameter.Name));
        }
    }

    // The argument Handler = ... of the attribute, else the attribute.
    private static Location? HandlerLocation(AttributeData attribute, Location? fallback)
    {
        var syntax = attribute.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;
        var argument = syntax?.ArgumentList?.Arguments.FirstOrDefault(item => item.NameEquals?.Name.Identifier.ValueText == "Handler");
        return argument?.GetLocation() ?? fallback;
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
