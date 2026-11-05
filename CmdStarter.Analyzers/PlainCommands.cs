using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace CyberInternauts.CmdStarter.Analyzers;

// The definition of a plain command, written once for the generator and the analyzers. It is the twin of Helper.IsPlainCommand and of the search of the handler in the library:
// a concrete class that is not an open generic type, does not implement IStarterCommand, carries [Command] and declares no [Command] method. Its handler is the public instance method
// that [Command(Handler = ...)] names, else the only public instance method that the class declares itself.
internal static class PlainCommands
{
    // The class qualifies as a plain command, whatever its handlers are: whether it has exactly one is a question of the handler (see FindHandlers).
    internal static bool IsCandidate(INamedTypeSymbol type, INamedTypeSymbol? commandAttribute, INamedTypeSymbol starterInterface)
    {
        // Roslyn does not report a static class as abstract, as reflection does: it is excluded by its own test.
        if (commandAttribute is null || type.TypeKind != TypeKind.Class || type.IsAbstract || type.IsStatic || IsOpenGeneric(type))
        {
            return false;
        }

        if (type.AllInterfaces.Contains(starterInterface, SymbolEqualityComparer.Default))
        {
            return false;
        }

        return HasAttribute(type, commandAttribute)
            && !type.GetMembers().OfType<IMethodSymbol>().Any(method => HasAttribute(method, commandAttribute));
    }

    // True when the type or one of the types that contain it has type parameters: the library cannot create it (ContainsGenericParameters).
    internal static bool IsOpenGeneric(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.TypeParameters.Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    // The name that [Command(Handler = ...)] gives, or null.
    internal static string? GetHandlerName(AttributeData commandAttribute) =>
        commandAttribute.NamedArguments.FirstOrDefault(argument => argument.Key == "Handler").Value.Value as string;

    // The methods that can be the handler of a class that qualifies. With a Handler name: the public instance methods of that name, declared by the class or inherited, but not the ones of object.
    // Without: the public instance methods that the class declares itself, but not accessors and operators, what the compiler generates, the redefinitions of the methods of object
    // and the parameterless Dispose or DisposeAsync of a disposable class. A usable class has exactly one.
    internal static ImmutableArray<IMethodSymbol> FindHandlers(INamedTypeSymbol type, string? handlerName)
    {
        if (handlerName is not null)
        {
            return FindDesignated(type, handlerName);
        }

        var isDisposable = type.AllInterfaces.Any(contract => contract.SpecialType == SpecialType.System_IDisposable);
        var isAsyncDisposable = type.AllInterfaces.Any(contract => contract.Name == "IAsyncDisposable" && contract.ContainingNamespace.ToDisplayString() == "System");
        return [.. type.GetMembers().OfType<IMethodSymbol>().Where(method =>
            method.MethodKind == MethodKind.Ordinary
            && method.DeclaredAccessibility == Accessibility.Public
            && !method.IsStatic
            && !method.IsImplicitlyDeclared
            && !OverridesObject(method)
            && !(isDisposable && method.Name == "Dispose" && method.Parameters.Length == 0)
            && !(isAsyncDisposable && method.Name == "DisposeAsync" && method.Parameters.Length == 0))];
    }

    private static ImmutableArray<IMethodSymbol> FindDesignated(INamedTypeSymbol type, string handlerName)
    {
        var found = new List<IMethodSymbol>();
        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            found.AddRange(current.GetMembers(handlerName).OfType<IMethodSymbol>().Where(method =>
                method.MethodKind == MethodKind.Ordinary && method.DeclaredAccessibility == Accessibility.Public && !method.IsStatic && !OverridesObject(method)));
        }

        // A method that a derived class overrides is one method: the most derived declaration is the one that counts.
        return [.. found.Where(method => !found.Any(other => other.OverriddenMethod is { } overridden && SymbolEqualityComparer.Default.Equals(overridden.OriginalDefinition, method.OriginalDefinition)))];
    }

    private static bool OverridesObject(IMethodSymbol method)
    {
        for (var current = method.OverriddenMethod; current is not null; current = current.OverriddenMethod)
        {
            if (current.ContainingType.SpecialType == SpecialType.System_Object)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attributeClass) => FindAttribute(symbol, attributeClass) is not null;

    internal static AttributeData? FindAttribute(ISymbol symbol, INamedTypeSymbol attributeClass) =>
        symbol.GetAttributes().FirstOrDefault(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeClass));
}
