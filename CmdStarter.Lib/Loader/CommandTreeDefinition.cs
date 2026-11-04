using CyberInternauts.CmdStarter.Interfaces;
using CyberInternauts.CmdStarter.Extensions;

namespace CyberInternauts.CmdStarter.Loader;

/// <summary>Defines command class placement in a fluent command tree.</summary>
public sealed class CommandTreeDefinition
{
    private readonly TreeNode<Type> node;
    private readonly SharedState state;
    private readonly NodeKind nodeKind;
    private readonly Type? commandType;
    private readonly List<GroupSegment> groups;

    internal CommandTreeDefinition(TreeNode<Type> node)
        : this(node, new SharedState(), NodeKind.Root, null, new List<GroupSegment>())
    {
    }

    private CommandTreeDefinition(TreeNode<Type> node, SharedState state, NodeKind nodeKind, Type? commandType, List<GroupSegment> groups)
    {
        this.node = node;
        this.state = state;
        this.nodeKind = nodeKind;
        this.commandType = commandType;
        this.groups = groups;
    }

    /// <summary>Adds a command type as a child of this level.</summary>
    /// <typeparam name="TCommand">Command type to add.</typeparam>
    /// <returns>This definition.</returns>
    public CommandTreeDefinition Add<TCommand>() where TCommand : IStarterCommand => Add(typeof(TCommand));

    /// <summary>Adds a command type and defines its children.</summary>
    /// <typeparam name="TCommand">Command type to add.</typeparam>
    /// <param name="children">Definition of the command's children.</param>
    /// <returns>This definition.</returns>
    public CommandTreeDefinition Add<TCommand>(Action<CommandTreeDefinition> children) where TCommand : IStarterCommand => Add(typeof(TCommand), children);

    /// <summary>Adds a command type as a child of this level.</summary>
    /// <param name="commandType">Command type to add.</param>
    /// <returns>This definition.</returns>
    public CommandTreeDefinition Add(Type commandType) => AddCore(commandType, null, false);

    /// <summary>Adds a command type and defines its children.</summary>
    /// <param name="commandType">Command type to add.</param>
    /// <param name="children">Definition of the command's children.</param>
    /// <returns>This definition.</returns>
    public CommandTreeDefinition Add(Type commandType, Action<CommandTreeDefinition> children) => AddCore(commandType, children, true);

    private CommandTreeDefinition AddCore(Type commandType, Action<CommandTreeDefinition>? children, bool hasChildren)
    {
        if (state.IsClosed)
        {
            throw new InvalidOperationException("The command tree definition can no longer be changed.");
        }

        ArgumentNullException.ThrowIfNull(commandType);
        if (hasChildren && children is null)
        {
            throw new ArgumentNullException(nameof(children));
        }

        if (!commandType.IsClass || commandType.IsAbstract || !commandType.IsAssignableTo(typeof(IStarterCommand)))
        {
            throw new ArgumentException($"Type '{commandType.FullName}' is not a non-abstract class implementing IStarterCommand.", nameof(commandType));
        }

        if (!state.Types.Add(commandType))
        {
            throw new ArgumentException($"Command type '{commandType.FullName}' is already in the tree.", nameof(commandType));
        }

        var childNode = node.AddChild(commandType);
        state.GroupPaths.Add(commandType, [.. groups]);
        var childDefinition = new CommandTreeDefinition(childNode, state, NodeKind.Command, commandType, new List<GroupSegment>());
        children?.Invoke(childDefinition);
        return this;
    }

    /// <summary>Adds a group without a command class at this level.</summary>
    /// <param name="name">Single segment group name.</param>
    /// <param name="children">Definition of the group's contents.</param>
    /// <returns>This definition.</returns>
    public CommandTreeDefinition Group(string name, Action<CommandTreeDefinition> children)
    {
        EnsureOpen();
        var commandName = ValidateName(name, nameof(name));
        ArgumentNullException.ThrowIfNull(children);

        var group = new GroupSegment(commandName);
        var nestedGroups = groups.Append(group).ToList();
        var commandCount = state.Types.Count;
        children(new CommandTreeDefinition(node, state, NodeKind.Group, null, nestedGroups));
        if (state.Types.Count == commandCount)
        {
            throw new ArgumentException($"Group '{name}' must contain at least one command.", nameof(name));
        }

        return this;
    }

    /// <summary>Renames the command represented by this node in the command tree.</summary>
    /// <remarks>The <see cref="StarterCommand.Name"/> property on the command instance is not changed; tree lookups and command paths use this name.</remarks>
    /// <param name="name">Single segment command name.</param>
    /// <returns>This definition.</returns>
    public CommandTreeDefinition WithName(string name)
    {
        EnsureOpen();
        if (nodeKind != NodeKind.Command || commandType is null)
        {
            throw new InvalidOperationException("Only a command node can be renamed.");
        }

        state.Names[commandType] = ValidateName(name, nameof(name));
        return this;
    }

    /// <summary>Adds aliases to the command or group represented by this node.</summary>
    /// <param name="aliases">Aliases to add.</param>
    /// <returns>This definition.</returns>
    public CommandTreeDefinition WithAliases(params string[] aliases)
    {
        EnsureOpen();
        ArgumentNullException.ThrowIfNull(aliases);
        if (nodeKind == NodeKind.Root)
        {
            throw new InvalidOperationException("The root of the tree has no aliases.");
        }

        foreach (var alias in aliases)
        {
            ValidateAlias(alias, nameof(aliases));
        }

        if (nodeKind == NodeKind.Group)
        {
            groups[^1].Aliases.AddRange(aliases);
        }
        else if (commandType is not null)
        {
            if (!state.Aliases.TryGetValue(commandType, out var commandAliases))
            {
                commandAliases = new List<string>();
                state.Aliases.Add(commandType, commandAliases);
            }

            commandAliases.AddRange(aliases);
        }

        return this;
    }

    private void EnsureOpen()
    {
        if (state.IsClosed)
        {
            throw new InvalidOperationException("The command tree definition can no longer be changed.");
        }
    }

    private static string ValidateName(string name, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("A command name must be a non-empty single segment.", parameterName);
        }

        return name.ToCommandName();
    }

    private static void ValidateAlias(string alias, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(alias) || alias.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("An alias must be a non-empty single segment.", parameterName);
        }
    }

    internal void Close() => state.IsClosed = true;

    internal TreeState ExportState() => new(state.GroupPaths.ToDictionary(pair => pair.Key, pair => pair.Value.Select(group => group.Copy()).ToList()),
        new Dictionary<Type, string>(state.Names),
        state.Aliases.ToDictionary(pair => pair.Key, pair => pair.Value.ToList()));

    private sealed class SharedState
    {
        public HashSet<Type> Types { get; } = new();
        public Dictionary<Type, List<GroupSegment>> GroupPaths { get; } = new();
        public Dictionary<Type, string> Names { get; } = new();
        public Dictionary<Type, List<string>> Aliases { get; } = new();
        public bool IsClosed { get; set; }
    }

    internal sealed record TreeState(Dictionary<Type, List<GroupSegment>> GroupPaths, Dictionary<Type, string> Names, Dictionary<Type, List<string>> Aliases);

    internal sealed class GroupSegment(string name)
    {
        public string Name { get; } = name;
        public List<string> Aliases { get; } = new();
        public GroupSegment Copy()
        {
            var copy = new GroupSegment(Name);
            copy.Aliases.AddRange(Aliases);
            return copy;
        }
    }

    private enum NodeKind
    {
        Root,
        Command,
        Group
    }
}
