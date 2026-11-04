using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CyberInternauts.CmdStarter.Analyzers;

/// <summary>
/// Collects the types of the values that the options and the arguments of the listed commands will have, so that the catalog registers a typed factory of symbols for each.
/// The collection over-approximates on purpose: a type registered for nothing costs one instantiation. A type that cannot be written as a type argument is not registered, without a diagnostic:
/// the library makes its factory by reflection at run time.
/// </summary>
internal static class SymbolTypeCollector
{
    // The type of every public instance property of a command or a container that has a getter and a setter, the inherited ones included (the library reads those too).
    // The properties of the types of the library itself are the ones of the interface and are never options.
    internal static void AddProperties(INamedTypeSymbol type, CommandCatalogGenerator.Symbols symbols, CommandCatalogGenerator.Found found)
    {
        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.ContainingAssembly, symbols.StarterCommand.ContainingAssembly))
            {
                continue;
            }

            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (!property.IsStatic && !property.IsIndexer && property.DeclaredAccessibility == Accessibility.Public && property.GetMethod is not null && property.SetMethod is not null)
                {
                    Add(property.Type, symbols, found);
                }
            }
        }
    }

    // The type of every parameter of the [Command] methods of the class, but the ones that never become an argument. A ref parameter is not a type argument.
    internal static void AddMethodParameters(INamedTypeSymbol type, CommandCatalogGenerator.Symbols symbols, CommandCatalogGenerator.Found found)
    {
        foreach (var method in type.GetMembers().OfType<IMethodSymbol>().Where(method => CommandCatalogGenerator.HasAttribute(method, symbols.CommandAttribute)))
        {
            foreach (var parameter in method.Parameters.Where(parameter => parameter.RefKind == RefKind.None))
            {
                Add(parameter.Type, symbols, found);
            }
        }
    }

    // The parameters of the delegate that HandlingMethod gives, when its body can be read: an expression whose static type is a concrete delegate type (a lambda, a cast, the creation of a delegate,
    // a field, a property, a call), or a method group of a single method.
    // The first class of the hierarchy that is in the compilation and declares the property decides; a command that does not override it uses the empty delegate of the library.
    // A class of the compilation always has a base type (object, or an error type, which has no syntax and ends the walk).
    internal static void AddHandlingMethod(INamedTypeSymbol type, string displayName, CommandCatalogGenerator.Symbols symbols, CommandCatalogGenerator.Found found, SourceProductionContext context)
    {
        for (var current = type; !current.DeclaringSyntaxReferences.IsEmpty; current = current.BaseType!)
        {
            var property = current.GetMembers("HandlingMethod").OfType<IPropertySymbol>().FirstOrDefault(candidate => !candidate.IsStatic);
            if (property is null)
            {
                continue;
            }

            var parameters = ReadParameters(property, symbols.Compilation);
            if (parameters.IsDefault)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.UnreadableHandlingMethod, property.Locations.FirstOrDefault(), displayName));
                return;
            }

            foreach (var parameter in parameters.Where(parameter => parameter.RefKind == RefKind.None))
            {
                Add(parameter.Type, symbols, found);
            }

            return;
        }
    }

    // The default (empty, not initialized) array when the delegate type cannot be read at compile time.
    private static ImmutableArray<IParameterSymbol> ReadParameters(IPropertySymbol property, Compilation compilation)
    {
        if (property.DeclaringSyntaxReferences[0].GetSyntax() is not PropertyDeclarationSyntax declaration)
        {
            return default;
        }

        var expression = ReadBody(declaration);
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        if (expression is null)
        {
            return default;
        }

        var model = compilation.GetSemanticModel(declaration.SyntaxTree);

        // Whatever the expression is, a lambda (its natural type), a cast, a creation, a field, a property or a call, a concrete delegate type is its static type.
        // System.Delegate and System.MulticastDelegate are classes, not delegate types: they tell nothing about the parameters.
        // The delegate that runs may be a variant of its static type (an Action<object> in an Action<string> field), and the binding reads the parameters of its target method:
        // the types registered are then those of the static type, which is harmless (a reference type uses the shared fallback).
        if (model.GetTypeInfo(expression).Type is INamedTypeSymbol { TypeKind: TypeKind.Delegate } delegateType)
        {
            return delegateType.DelegateInvokeMethod!.Parameters;
        }

        // The semantic model does not bind a method group converted to Delegate to a symbol (it reports a failed overload resolution), but it gives the group:
        // a single method is the one whose natural type the compiler used. A field, a property or an overloaded name is not.
        if (expression is IdentifierNameSyntax or MemberAccessExpressionSyntax)
        {
            var methods = model.GetMemberGroup(expression).OfType<IMethodSymbol>().ToArray();
            return methods.Length == 1 ? methods[0].Parameters : default;
        }

        return default;
    }
    // An expression body, or a getter made of an expression body or of a single return.
    private static ExpressionSyntax? ReadBody(PropertyDeclarationSyntax declaration)
    {
        if (declaration.ExpressionBody is not null)
        {
            return declaration.ExpressionBody.Expression;
        }

        var getter = declaration.ChildNodes().OfType<AccessorListSyntax>().SelectMany(list => list.Accessors).FirstOrDefault(accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
        if (getter?.ExpressionBody is not null)
        {
            return getter.ExpressionBody.Expression;
        }

        return getter?.Body is { Statements.Count: 1 } body && body.Statements[0] is ReturnStatementSyntax returned ? returned.Expression : null;
    }

    private static void Add(ITypeSymbol type, CommandCatalogGenerator.Symbols symbols, CommandCatalogGenerator.Found found)
    {
        if (SymbolEqualityComparer.Default.Equals(type, symbols.CancellationToken) || SymbolEqualityComparer.Default.Equals(type, symbols.CommandConsole))
        {
            return;
        }

        var diagnosticIds = new List<string>();
        if (IsWritable(type, symbols, diagnosticIds))
        {
            found.SymbolTypes.Add(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            found.DiagnosticIds.UnionWith(diagnosticIds);
        }
    }

    // Whether the generated code can write the type as a type argument. The own diagnostic ids of the [Obsolete] types it names are collected for the #pragma of the file.
    private static bool IsWritable(ITypeSymbol type, CommandCatalogGenerator.Symbols symbols, List<string> diagnosticIds)
    {
        if (type is IArrayTypeSymbol array)
        {
            return IsWritable(array.ElementType, symbols, diagnosticIds);
        }

        // Type parameters, pointers, function pointers and dynamic are not named types; an error type is.
        if (type is not INamedTypeSymbol named || named.TypeKind == TypeKind.Error || named.IsRefLikeType)
        {
            return false;
        }

        for (var current = named; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility is Accessibility.Private or Accessibility.Protected or Accessibility.ProtectedAndInternal || current.IsFileLocal)
            {
                return false;
            }

            if (CommandCatalogGenerator.IsObsoleteError(current, symbols.Obsolete) || CommandCatalogGenerator.HasAttribute(current, symbols.Experimental))
            {
                return false;
            }

            var diagnosticId = CommandCatalogGenerator.ObsoleteDiagnosticId(current, symbols);
            if (diagnosticId is not null)
            {
                if (!CommandCatalogGenerator.IsIdentifier(diagnosticId))
                {
                    return false;
                }

                diagnosticIds.Add(diagnosticId);
            }

            if (current.TypeArguments.Any(argument => !IsWritable(argument, symbols, diagnosticIds)))
            {
                return false;
            }
        }

        return true;
    }
}
