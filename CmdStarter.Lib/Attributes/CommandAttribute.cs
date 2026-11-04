namespace CyberInternauts.CmdStarter.Attributes;

/// <summary>Marks a class or method as a command in the method-based command model.</summary>
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
