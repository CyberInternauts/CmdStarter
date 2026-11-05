namespace CyberInternauts.CmdStarter.Tests.Commands.ExecutionPaths;

// Gives its name and description to the base constructor: it has no parameterless constructor, so it is only ever created by hand.
public sealed class NamedCommand(string name, string? description = null) : StarterCommand(name, description)
{
}
