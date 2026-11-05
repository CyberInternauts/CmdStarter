using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter.Exceptions;
using CyberInternauts.CmdStarter.Interfaces;
using static CyberInternauts.CmdStarter.Reflection.Helper;

namespace CyberInternauts.CmdStarter.Loader;

internal static class MethodCommandsLoader
{
    // A class that is marked [Command] but is a plain command is not a group of the method model: the class model has it.
    internal static bool DeclaresCommands([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type) =>
        (Attribute.IsDefined(type, typeof(CommandAttribute)) && !IsPlainCommand(type))
        || type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Any(method => Attribute.IsDefined(method, typeof(CommandAttribute)));

    // The commands of a class in the order of their declaration (the metadata token). Without dynamic code (Native AOT), a method has no metadata token:
    // the order is the one that GetMethods gives, which is not necessarily the order of the declarations.
    internal static IReadOnlyList<MethodInfo> GetCommandMethods([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type, bool dynamicCodeSupported)
    {
        var methods = type
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => Attribute.IsDefined(method, typeof(CommandAttribute)));
        return [.. (dynamicCodeSupported ? methods.OrderBy(method => method.MetadataToken) : methods)];
    }

    internal static void Validate([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type)
    {
        var declared = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        var methods = declared.Where(method => Attribute.IsDefined(method, typeof(CommandAttribute))).ToArray();
        var classAttribute = type.GetCustomAttribute<CommandAttribute>();
        void Fail(string message) => throw new InvalidAttributeException($"{message} Type '{type.FullName}', method '{methods.FirstOrDefault()?.Name ?? "<none>"}'.");
        if (typeof(IStarterCommand).IsAssignableFrom(type) && (methods.Length > 0 || classAttribute is not null)) Fail("A class cannot use both command models.");
        if (classAttribute?.Handler is not null) Fail("Handler names the handler of a plain command: it cannot be used on a class of command methods.");
        if (classAttribute is not null && methods.Length == 0) Fail("A command group must declare at least one command method.");
        if (type.IsAbstract && !type.IsSealed && methods.Any(method => !method.IsStatic)) Fail("An abstract command class cannot declare instance commands.");
        foreach (var method in methods)
        {
            if (!method.IsPublic) throw new InvalidAttributeException($"Command method must be public. Type '{type.FullName}', method '{method.Name}'.");
            if (method.GetCustomAttribute<CommandAttribute>()!.Handler is not null) throw new InvalidAttributeException($"Handler names the handler of a plain command: it cannot be used on a command method. Type '{type.FullName}', method '{method.Name}'.");
            ValidateShape(method, type);
        }
    }

    // The handler of a plain command: the public instance method that [Command(Handler = ...)] names, else the only public instance method that the class declares itself.
    // Throws InvalidAttributeException for a class that has none, several, or a method that a command cannot be.
    internal static MethodInfo GetPlainCommandMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
        var handlerName = type.GetCustomAttribute<CommandAttribute>()!.Handler;
        var method = handlerName is null ? FindConventionalHandler(type) : FindDesignatedHandler(type, handlerName);
        ValidateShape(method, type);
        return method;
    }

    private static MethodInfo FindConventionalHandler([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
        var isDisposable = typeof(IDisposable).IsAssignableFrom(type);
        var isAsyncDisposable = typeof(IAsyncDisposable).IsAssignableFrom(type);

        // Not handlers: accessors and operators, what the compiler generates, the redefinitions of the methods of object, and the disposal of a disposable command.
        var candidates = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName
                && !method.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
                && method.GetBaseDefinition().DeclaringType != typeof(object)
                && !(isDisposable && method.Name == nameof(IDisposable.Dispose) && method.GetParameters().Length == 0)
                && !(isAsyncDisposable && method.Name == nameof(IAsyncDisposable.DisposeAsync) && method.GetParameters().Length == 0))
            .ToArray();
        if (candidates.Length != 1)
        {
            var found = candidates.Length == 0 ? string.Empty : $" Public instance methods: {string.Join(", ", candidates.Select(method => method.Name))}.";
            throw new InvalidAttributeException($"A [Command] class must declare command methods or exactly one public instance method (or name its handler with Handler).{found} Type '{type.FullName}'.");
        }

        return candidates[0];
    }

    private static MethodInfo FindDesignatedHandler([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type, string handlerName)
    {
        var matches = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => string.Equals(method.Name, handlerName, StringComparison.Ordinal) && method.GetBaseDefinition().DeclaringType != typeof(object))
            .ToArray();
        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new InvalidAttributeException($"Handler '{handlerName}' does not name a public instance method. Type '{type.FullName}'."),
            _ => throw new InvalidAttributeException($"Handler '{handlerName}' is ambiguous: {matches.Length} public instance methods have this name. Type '{type.FullName}'.")
        };
    }

    private static void ValidateShape(MethodInfo method, Type type)
    {
        if (method.IsGenericMethodDefinition || method.ContainsGenericParameters) throw new InvalidAttributeException($"Command method cannot be generic. Type '{type.FullName}', method '{method.Name}'.");
        if (method.GetParameters().Any(parameter => parameter.ParameterType.IsByRef || parameter.IsIn || parameter.IsOut)) throw new InvalidAttributeException($"Command method cannot use ref, out or in parameters. Type '{type.FullName}', method '{method.Name}'.");
    }
}
