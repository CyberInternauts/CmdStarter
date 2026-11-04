using Microsoft.CodeAnalysis;

namespace CyberInternauts.CmdStarter.Analyzers;

internal static class Descriptors
{
    private static DiagnosticDescriptor Create(string id, string title, string message, DiagnosticSeverity severity = DiagnosticSeverity.Warning) => new(
        id,
        title,
        message,
        "Usage",
        severity,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor ParentCycle = Create("CMDS0001", "Parent cycle", "Parent chain for '{0}' returns to itself");
    internal static readonly DiagnosticDescriptor MultipleParents = Create("CMDS0002", "Multiple parent attributes", "Type '{0}' declares more than one parent attribute");
    internal static readonly DiagnosticDescriptor EmptyCompletion = Create("CMDS0003", "Auto-complete has no values", "Auto-complete on '{0}' produces no completion values");
    internal static readonly DiagnosticDescriptor UnsupportedCompletion = Create("CMDS0004", "Unsupported auto-complete type", "Type '{0}' is not a supported auto-complete source");
    internal static readonly DiagnosticDescriptor EmptyCommandName = Create("CMDS0005", "Empty command name", "Command name cannot be empty or whitespace");
    internal static readonly DiagnosticDescriptor NonPublicMethod = Create("CMDS0006", "Command method is not public", "Command method '{0}' must be public");
    internal static readonly DiagnosticDescriptor GenericMethod = Create("CMDS0007", "Command method is generic", "Command method '{0}' cannot be generic");
    internal static readonly DiagnosticDescriptor ByRefParameter = Create("CMDS0008", "Command parameter is by reference", "Parameter '{0}' cannot be ref, out, or in");
    internal static readonly DiagnosticDescriptor MixedModels = Create("CMDS0009", "Mixed command models", "Type '{0}' cannot implement IStarterCommand and declare command methods");
    internal static readonly DiagnosticDescriptor AbstractInstanceMethod = Create("CMDS0010", "Abstract instance command", "Abstract type '{0}' cannot declare an instance command method");
    internal static readonly DiagnosticDescriptor EmptyGroup = Create("CMDS0011", "Empty command group", "Command group '{0}' must declare at least one command method");
    internal static readonly DiagnosticDescriptor HiddenGlobalOption = Create("CMDS0012", "Global option is hidden", "Option '{0}' hides an option with the same name or alias from a global options container");
    internal static readonly DiagnosticDescriptor EmptyGlobalOptions = Create("CMDS0013", "Empty global options container", "Global options container '{0}' declares no option properties");
    internal static readonly DiagnosticDescriptor NotInCatalog = Create("CMDS0014", "Type is not in the command catalog", "'{0}' is not in the command catalog: {1}", DiagnosticSeverity.Info);
    internal static readonly DiagnosticDescriptor UnreadableHandlingMethod = Create("CMDS0015", "HandlingMethod cannot be read at compile time", "The delegate type of HandlingMethod of '{0}' cannot be read at compile time: its argument types are created by reflection at run time", DiagnosticSeverity.Info);
    internal static readonly DiagnosticDescriptor LostValueType = Create("CMDS0016", "Value type cannot be registered in the command catalog", "The value type '{0}' used by '{1}' cannot be registered in the command catalog ({2}): under Native AOT its option or argument is not supported. Make the type reachable from a top-level class.", DiagnosticSeverity.Info);
}
