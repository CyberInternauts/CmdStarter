using System.CommandLine;
using CyberInternauts.CmdStarter.Exceptions;

namespace CyberInternauts.CmdStarter.Loader;

internal static class SymbolNamesValidator
{
    internal static void Validate(Command root)
    {
        ValidateCommand(root);
    }

    private static void ValidateCommand(Command command)
    {
        var commandNames = new Dictionary<string, Command>(StringComparer.Ordinal);
        foreach (var member in new[] { command }.Concat(command.Subcommands))
        {
            foreach (var token in GetTokens(member.Name, member.Aliases))
            {
                if (commandNames.TryGetValue(token, out var existing) && !ReferenceEquals(existing, member))
                {
                    var firstPath = GetPath(existing);
                    var secondPath = GetPath(member);
                    throw new InvalidAttributeException($"Command name '{token}' is used by both '{firstPath}' and '{secondPath}'.");
                }

                commandNames.TryAdd(token, member);
            }
        }

        var optionNames = new Dictionary<string, Option>(StringComparer.Ordinal);
        foreach (var option in command.Options)
        {
            foreach (var token in GetTokens(option.Name, option.Aliases))
            {
                if (optionNames.TryGetValue(token, out var existing) && !ReferenceEquals(existing, option))
                {
                    throw new InvalidAttributeException($"Option name '{token}' is used by both '{existing.Name}' and '{option.Name}' on command '{GetPath(command)}'.");
                }

                optionNames.TryAdd(token, option);
            }
        }

        foreach (var child in command.Subcommands)
        {
            ValidateCommand(child);
        }
    }

    private static IEnumerable<string> GetTokens(string name, IEnumerable<string> aliases) =>
        new[] { name }.Concat(aliases).Distinct(StringComparer.Ordinal);

    private static string GetPath(Command command)
    {
        var path = Starter.GetCommandPath(command);
        return path.Length == 0 ? command.Name : path;
    }
}
