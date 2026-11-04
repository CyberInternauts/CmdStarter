using System.Reflection;
using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter.Exceptions;
using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Loader;

internal static class MethodCommandsLoader
{
    internal static bool DeclaresCommands(Type type) => Attribute.IsDefined(type, typeof(CommandAttribute))
        || type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Any(method => Attribute.IsDefined(method, typeof(CommandAttribute)));

    internal static IReadOnlyList<MethodInfo> GetCommandMethods(Type type) => type
        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Where(method => Attribute.IsDefined(method, typeof(CommandAttribute)))
        .OrderBy(method => method.MetadataToken)
        .ToArray();

    internal static void Validate(Type type)
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
