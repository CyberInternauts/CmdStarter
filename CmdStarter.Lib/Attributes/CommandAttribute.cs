namespace CyberInternauts.CmdStarter.Attributes;

/// <summary>Marks a class or method as a command in the method-based command model.</summary>
/// <remarks>
/// Names may contain space-separated paths. Path segments are resolved below existing commands or groups,
/// with missing intermediate segments created as implicit groups. Groups may be nested below class-based commands,
/// including commands that already have an action; those commands can run themselves and expose subcommands.
/// Implicit groups remain visible when command state filtering is applied.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class CommandAttribute : Attribute
{
    /// <summary>Creates a command whose name is inferred from its target.</summary>
    public CommandAttribute() { }

    /// <summary>Creates a command with an explicit name.</summary>
    /// <param name="name">The command name.</param>
    public CommandAttribute(string name) => Name = name;

    /// <summary>Gets the optional explicit command name.</summary>
    public string? Name { get; }
}
