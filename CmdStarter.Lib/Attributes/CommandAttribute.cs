namespace CyberInternauts.CmdStarter.Attributes;

/// <summary>Marks a method as a command, a class of command methods as a group of commands, or a class without command methods as a command itself (a plain command).</summary>
/// <remarks>
/// On a method, it makes the method a command of the method-based model. On a class that declares such methods, it names their group.
/// On a class that declares none, the class is a plain command of the class-based model: its handler is its only public instance method,
/// or the one that <see cref="Handler"/> names, and its public read-write properties are its options.
/// Names of methods and groups may contain space-separated paths. Path segments are resolved below existing commands or groups,
/// with missing intermediate segments created as implicit groups. Groups may be nested below class-based commands,
/// including commands that already have an action; those commands can run themselves and expose subcommands.
/// Implicit groups remain visible when command state filtering is applied. The name of a plain command is a single segment.
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

    /// <summary>
    /// Gets the name of the handler of a plain command, for example <c>Handler = nameof(Run)</c>: the public instance method of that name, declared by the class or inherited,
    /// which must be unique. Without it, the handler is the only public instance method that the class declares itself.
    /// It is an error on a method and on a class that declares command methods.
    /// </summary>
    public string? Handler { get; init; }
}
