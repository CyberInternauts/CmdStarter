using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter.Exceptions;
using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Loader;

internal static class MethodCommandsLoader
{
    internal static bool DeclaresCommands([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type) => Attribute.IsDefined(type, typeof(CommandAttribute))
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
        void Fail(string message) => throw new InvalidAttributeException($"{message} Type '{type.FullName}', method '{methods.FirstOrDefault()?.Name ?? "<none>"}'.");
        if (Attribute.IsDefined(type, typeof(CommandAttribute)) && methods.Length == 0) Fail("A command group must declare at least one command method.");
        if (typeof(IStarterCommand).IsAssignableFrom(type) && methods.Length > 0) Fail("A class cannot use both command models.");
        if (type.IsAbstract && !type.IsSealed && methods.Any(method => !method.IsStatic)) Fail("An abstract command class cannot declare instance commands.");
        foreach (var method in methods)
        {
            if (!method.IsPublic) throw new InvalidAttributeException($"Command method must be public. Type '{type.FullName}', method '{method.Name}'.");
            if (method.IsGenericMethodDefinition || method.ContainsGenericParameters) throw new InvalidAttributeException($"Command method cannot be generic. Type '{type.FullName}', method '{method.Name}'.");
            if (method.GetParameters().Any(parameter => parameter.ParameterType.IsByRef || parameter.IsIn || parameter.IsOut)) throw new InvalidAttributeException($"Command method cannot use ref, out or in parameters. Type '{type.FullName}', method '{method.Name}'.");
        }
    }
}
