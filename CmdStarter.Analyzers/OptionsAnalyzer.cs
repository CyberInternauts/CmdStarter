using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CyberInternauts.CmdStarter.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OptionsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptors.HiddenGlobalOption, Descriptors.EmptyGlobalOptions];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var starterInterface = start.Compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Interfaces.IStarterCommand");
            var globalInterface = start.Compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Interfaces.IGlobalOptionsContainer");
            var option = start.Compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Attributes.OptionAttribute");
            var notOption = start.Compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Attributes.NotOptionAttribute");
            var excluded = start.Compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Attributes.AllOptionsExcludedAttribute");
            var alias = start.Compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Attributes.AliasAttribute");
            var commandAttribute = start.Compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Attributes.CommandAttribute");
            if (starterInterface is null || globalInterface is null || option is null || notOption is null || excluded is null || alias is null)
            {
                return;
            }

            var globalNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var globalType in GetAllTypes(start.Compilation.Assembly.GlobalNamespace).Where(candidate => Implements(candidate, globalInterface)))
            {
                foreach (var property in GetOptions(globalType, starterInterface, globalInterface, option, notOption, excluded, isPlain: false))
                {
                    AddNames(globalNames, property.Name, property.GetAttributes(), alias);
                }
            }

            start.RegisterSymbolAction(action => AnalyzeType(action, starterInterface, globalInterface, option, notOption, excluded, alias, commandAttribute, globalNames), SymbolKind.NamedType);
        });
    }

    private static void AnalyzeType(SymbolAnalysisContext context, INamedTypeSymbol starterInterface, INamedTypeSymbol globalInterface, INamedTypeSymbol optionAttribute, INamedTypeSymbol notOptionAttribute, INamedTypeSymbol excludedAttribute, INamedTypeSymbol aliasAttribute, INamedTypeSymbol? commandAttribute, HashSet<string> globalNames)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class)
        {
            return;
        }

        if (!type.IsAbstract && Implements(type, globalInterface) && GetOptions(type, starterInterface, globalInterface, optionAttribute, notOptionAttribute, excludedAttribute, isPlain: false).Length == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.EmptyGlobalOptions, type.Locations.FirstOrDefault(), type.Name));
        }

        // A plain command has its options too: its public read-write properties, and the [Option] parameters of its handler.
        var isPlain = PlainCommands.IsCandidate(type, commandAttribute, starterInterface);
        if (Implements(type, starterInterface) || isPlain)
        {
            foreach (var property in GetOptions(type, starterInterface, globalInterface, optionAttribute, notOptionAttribute, excludedAttribute, isPlain))
            {
                if (Intersects(globalNames, GetNames(property.Name, property.GetAttributes(), aliasAttribute)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.HiddenGlobalOption, property.Locations.FirstOrDefault(), property.Name));
                }
            }
        }

        if (commandAttribute is not null)
        {
            var handlers = type.GetMembers().OfType<IMethodSymbol>().Where(method => method.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, commandAttribute)));
            if (isPlain)
            {
                handlers = PlainCommands.FindHandlers(type, PlainCommands.GetHandlerName(PlainCommands.FindAttribute(type, commandAttribute)!), context.Compilation);
            }

            foreach (var method in handlers)
            {
                foreach (var parameter in method.Parameters.Where(parameter => parameter.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, optionAttribute))))
                {
                    if (Intersects(globalNames, GetNames(parameter.Name, parameter.GetAttributes(), aliasAttribute)))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptors.HiddenGlobalOption, parameter.Locations.FirstOrDefault(), parameter.Name));
                    }
                }
            }
        }
    }

    private static IPropertySymbol[] GetOptions(INamedTypeSymbol type, INamedTypeSymbol starterInterface, INamedTypeSymbol globalInterface, INamedTypeSymbol optionAttribute, INamedTypeSymbol notOptionAttribute, INamedTypeSymbol excludedAttribute, bool isPlain)
    {
        var properties = type.GetMembers().OfType<IPropertySymbol>().Concat(GetBaseProperties(type)).Where(property =>
            property.DeclaredAccessibility == Accessibility.Public
            && !property.IsStatic
            && property.GetMethod is not null
            && property.SetMethod is not null
            && (isPlain || Implements(property.ContainingType, starterInterface) || Implements(property.ContainingType, globalInterface))
            && property.ContainingType is { } declaringType
            && !declaringType.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, excludedAttribute))
            && !(!isPlain && Implements(declaringType, starterInterface) && starterInterface.GetMembers().OfType<IPropertySymbol>().Any(item => StringComparer.Ordinal.Equals(item.Name, property.Name))))
            .ToArray();
        var hasOption = properties.Any(property => property.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, optionAttribute)));
        return [.. properties.Where(property =>
        {
            var attrs = property.GetAttributes();
            var hasOptionAttr = attrs.Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, optionAttribute));
            var hasNotOption = attrs.Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, notOptionAttribute));
            return !hasNotOption && (!hasOption || hasOptionAttr);
        })];
    }

    private static IEnumerable<IPropertySymbol> GetBaseProperties(INamedTypeSymbol type)
    {
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            foreach (var property in baseType.GetMembers().OfType<IPropertySymbol>())
            {
                yield return property;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol root)
    {
        foreach (var type in root.GetTypeMembers())
        {
            yield return type;
            foreach (var nested in GetNestedTypes(type))
            {
                yield return nested;
            }
        }

        foreach (var child in root.GetNamespaceMembers())
        {
            foreach (var type in GetAllTypes(child))
            {
                yield return type;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            yield return nested;
            foreach (var child in GetNestedTypes(nested))
            {
                yield return child;
            }
        }
    }

    private static void AddNames(HashSet<string> names, string name, ImmutableArray<AttributeData> attributes, INamedTypeSymbol aliasAttribute)
    {
        foreach (var value in GetNames(name, attributes, aliasAttribute))
        {
            names.Add(value);
        }
    }

    private static IEnumerable<string> GetNames(string name, ImmutableArray<AttributeData> attributes, INamedTypeSymbol aliasAttribute)
    {
        yield return "--" + NameConversion.PascalToKebabCase(name);
        foreach (var attribute in attributes.Where(item => SymbolEqualityComparer.Default.Equals(item.AttributeClass, aliasAttribute)))
        {
            var usePrefix = attribute.ConstructorArguments.Length > 1 && true.Equals(attribute.ConstructorArguments[0].Value);
            var prefix = attribute.NamedArguments.FirstOrDefault(item => StringComparer.Ordinal.Equals(item.Key, "Prefix")).Value.Value as string ?? "-";
            var values = attribute.ConstructorArguments.Length > 1 ? attribute.ConstructorArguments[1] : attribute.ConstructorArguments.FirstOrDefault();
            // A null array names no alias, and it has no Values.
            if (values.Kind == TypedConstantKind.Array && !values.IsNull)
            {
                foreach (var alias in values.Values)
                {
                    if (alias.Value is string text)
                    {
                        yield return (usePrefix ? prefix : string.Empty) + text;
                    }
                }
            }
        }
    }

    private static bool Intersects(HashSet<string> globalNames, IEnumerable<string> localNames) => localNames.Any(globalNames.Contains);

    private static bool Implements(INamedTypeSymbol type, INamedTypeSymbol iface) => type.AllInterfaces.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, iface));
}
