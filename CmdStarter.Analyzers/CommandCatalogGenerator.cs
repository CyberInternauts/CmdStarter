using System.Globalization;
using Microsoft.CodeAnalysis;

namespace CyberInternauts.CmdStarter.Analyzers;

/// <summary>
/// Writes, in every compilation that references CmdStarter.Lib, the catalog of the commands, of the classes of method commands and of the global options containers
/// of the assembly. The catalog follows the predicates of the scan that the library makes by reflection and validates nothing: the engine still validates.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class CommandCatalogGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, static (productionContext, compilation) => Generate(productionContext, compilation));
    }

    private static void Generate(SourceProductionContext context, Compilation compilation)
    {
        var starterCommand = compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Interfaces.IStarterCommand");
        var catalogAttribute = compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Catalog.CommandCatalogAttribute");
        if (starterCommand is null || catalogAttribute is null)
        {
            return; // The library is not referenced, or it is a version without catalogs.
        }

        if (compilation.Assembly.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, catalogAttribute)))
        {
            return; // A catalog written by hand wins: this is also the way to turn the generator off.
        }

        var symbols = new Symbols(compilation, starterCommand);
        var found = new Found();
        VisitNamespace(compilation.SourceModule.GlobalNamespace, symbols, found, context);

        var version = typeof(CommandCatalogGenerator).Assembly.GetName().Version!.ToString(); // A compiled assembly always has a version.
        context.AddSource(
            CatalogSource.FileName,
            CatalogSource.Write(
                CatalogSource.ClassName(compilation.AssemblyName),
                version,
                Sorted(found.Commands),
                Sorted(found.MethodClasses),
                Sorted(found.GlobalOptions),
                Sorted(found.SymbolTypes.ToList()),
                Sorted(found.DiagnosticIds.Where(id => id is not ("CS0612" or "CS0618")).ToList())));
    }

    private static List<string> Sorted(List<string> names)
    {
        names.Sort(StringComparer.Ordinal);
        return names;
    }

    private static void VisitNamespace(INamespaceSymbol container, Symbols symbols, Found found, SourceProductionContext context)
    {
        foreach (var member in container.GetMembers())
        {
            if (member is INamespaceSymbol nested)
            {
                VisitNamespace(nested, symbols, found, context);
            }
            else if (member is INamedTypeSymbol type)
            {
                VisitType(type, symbols, found, context);
            }
        }
    }

    private static void VisitType(INamedTypeSymbol type, Symbols symbols, Found found, SourceProductionContext context)
    {
        Classify(type, symbols, found, context);
        foreach (var nested in type.GetTypeMembers())
        {
            VisitType(nested, symbols, found, context);
        }
    }

    private static void Classify(INamedTypeSymbol type, Symbols symbols, Found found, SourceProductionContext context)
    {
        if (type.TypeKind != TypeKind.Class)
        {
            return;
        }

        // The predicates of the scan: a command is a concrete class with a namespace, a class of method commands has a namespace, a container needs neither.
        var hasNamespace = !type.ContainingNamespace.IsGlobalNamespace;
        var isCommand = hasNamespace && !type.IsAbstract && Implements(type, symbols.StarterCommand);
        var isMethodClass = hasNamespace && DeclaresCommands(type, symbols.CommandAttribute);
        var isContainer = Implements(type, symbols.GlobalOptionsContainer);
        if (!isCommand && !isMethodClass && !isContainer)
        {
            return;
        }

        var name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var reason = LeftOutReason(type, symbols);
        if (reason is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.NotInCatalog, type.Locations.FirstOrDefault(), name, reason));
            return;
        }

        // An obsolete type with its own diagnostic id is reported under that id, not under CS0618: the generated file has to switch it off too.
        for (var current = type; current is not null; current = current.ContainingType)
        {
            var diagnosticId = ObsoleteDiagnosticId(current, symbols);
            if (diagnosticId is not null)
            {
                found.DiagnosticIds.Add(diagnosticId);
            }
        }

        if (isCommand)
        {
            found.Commands.Add(name);
            SymbolTypeCollector.AddProperties(type, symbols, found);
            SymbolTypeCollector.AddHandlingMethod(type, name, symbols, found, context);
        }

        if (isMethodClass)
        {
            found.MethodClasses.Add(name);
            SymbolTypeCollector.AddMethodParameters(type, symbols, found);
        }

        if (isContainer)
        {
            found.GlobalOptions.Add(name);
            SymbolTypeCollector.AddProperties(type, symbols, found);
        }
    }

    private static bool Implements(INamedTypeSymbol type, INamedTypeSymbol? contract) =>
        contract is not null && type.AllInterfaces.Contains(contract, SymbolEqualityComparer.Default);

    // [Command] is not inherited: only what the class itself declares counts, on the class or on one of its methods (any accessibility, instance or static).
    private static bool DeclaresCommands(INamedTypeSymbol type, INamedTypeSymbol? commandAttribute) =>
        HasAttribute(type, commandAttribute) || type.GetMembers().OfType<IMethodSymbol>().Any(method => HasAttribute(method, commandAttribute));

    internal static bool HasAttribute(ISymbol symbol, INamedTypeSymbol? attributeClass) => FindAttribute(symbol, attributeClass) is not null;

    // The attribute class is compared as a symbol, never by name. A library that lacks the attribute class (a null here) declares nothing.
    private static AttributeData? FindAttribute(ISymbol symbol, INamedTypeSymbol? attributeClass) =>
        attributeClass is null ? null : symbol.GetAttributes().FirstOrDefault(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeClass));

    // The generated class is a top-level class of the same assembly: it can only name a type that it can reach, and the catalog may not break a valid compilation.
    private static string? LeftOutReason(INamedTypeSymbol type, Symbols symbols)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            var accessibility = current.DeclaredAccessibility switch
            {
                Accessibility.Private => "private",
                Accessibility.Protected => "protected",
                Accessibility.ProtectedAndInternal => "private protected",
                _ => null,
            };
            if (accessibility is not null)
            {
                return string.Format(CultureInfo.InvariantCulture, "'{0}' is {1} and cannot be named from the catalog", current.Name, accessibility);
            }

            if (current.IsFileLocal)
            {
                return string.Format(CultureInfo.InvariantCulture, "'{0}' is file-local and cannot be named from the catalog", current.Name);
            }
        }

        for (var outer = type.ContainingType; outer is not null; outer = outer.ContainingType)
        {
            if (outer.TypeParameters.Length > 0)
            {
                return string.Format(CultureInfo.InvariantCulture, "it is nested in the generic type '{0}'", outer.Name);
            }
        }

        if (type.TypeParameters.Length > 0)
        {
            return "open generic type definition";
        }

        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (IsObsoleteError(current, symbols.Obsolete))
            {
                return string.Format(CultureInfo.InvariantCulture, "'{0}' is obsolete as an error", current.Name);
            }

            if (HasAttribute(current, symbols.Experimental))
            {
                return string.Format(CultureInfo.InvariantCulture, "'{0}' is experimental", current.Name);
            }

            var diagnosticId = ObsoleteDiagnosticId(current, symbols);
            if (diagnosticId is not null && !IsIdentifier(diagnosticId))
            {
                return string.Format(CultureInfo.InvariantCulture, "'{0}' is obsolete with the diagnostic id '{1}', which cannot be suppressed in the catalog", current.Name, diagnosticId);
            }
        }

        return null;
    }

    // The DiagnosticId of [Obsolete], when the type has one: the compiler reports the use of the type under that id instead of CS0618.
    internal static string? ObsoleteDiagnosticId(INamedTypeSymbol type, Symbols symbols)
    {
        var attribute = FindAttribute(type, symbols.Obsolete);
        var named = attribute?.NamedArguments.FirstOrDefault(argument => argument.Key == "DiagnosticId").Value.Value as string;
        return string.IsNullOrEmpty(named) ? null : named;
    }

    // What a #pragma warning disable accepts as an id: [A-Za-z_][A-Za-z0-9_]*.
    internal static bool IsIdentifier(string text) =>
        (IsAsciiLetter(text[0]) || text[0] == '_') && text.All(character => IsAsciiLetter(character) || character == '_' || (character >= '0' && character <= '9'));

    private static bool IsAsciiLetter(char character) => (character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z');

    // [Obsolete("message", true)]: using the type is a compilation error, so the generated code could not name it.
    internal static bool IsObsoleteError(INamedTypeSymbol type, INamedTypeSymbol? obsolete) =>
        FindAttribute(type, obsolete) is { ConstructorArguments.Length: 2 } attribute && Equals(attribute.ConstructorArguments[1].Value, true);

    internal sealed class Symbols
    {
        internal Symbols(Compilation compilation, INamedTypeSymbol starterCommand)
        {
            Compilation = compilation;
            StarterCommand = starterCommand;
            GlobalOptionsContainer = compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Interfaces.IGlobalOptionsContainer");
            CommandAttribute = compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.Attributes.CommandAttribute");
            Obsolete = compilation.GetTypeByMetadataName("System.ObsoleteAttribute");
            Experimental = compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.ExperimentalAttribute");
            CancellationToken = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
            CommandConsole = compilation.GetTypeByMetadataName("CyberInternauts.CmdStarter.CommandConsole");
        }

        internal Compilation Compilation { get; }

        // The two types that never become an argument: the token and the console of the execution.
        internal INamedTypeSymbol? CancellationToken { get; }

        internal INamedTypeSymbol? CommandConsole { get; }

        internal INamedTypeSymbol StarterCommand { get; }

        internal INamedTypeSymbol? GlobalOptionsContainer { get; }

        internal INamedTypeSymbol? CommandAttribute { get; }

        internal INamedTypeSymbol? Obsolete { get; }

        internal INamedTypeSymbol? Experimental { get; }
    }

    internal sealed class Found
    {
        internal List<string> Commands { get; } = new();

        internal List<string> MethodClasses { get; } = new();

        internal List<string> GlobalOptions { get; } = new();

        internal HashSet<string> SymbolTypes { get; } = new(StringComparer.Ordinal);

        internal HashSet<string> DiagnosticIds { get; } = new(StringComparer.Ordinal);
    }
}
