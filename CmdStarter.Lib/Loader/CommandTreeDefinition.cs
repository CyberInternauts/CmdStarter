using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Loader;

/// <summary>Defines command class placement in a fluent command tree.</summary>
public sealed class CommandTreeDefinition
{
    private readonly TreeNode<Type> node;
    private readonly SharedState state;

    internal CommandTreeDefinition(TreeNode<Type> node)
        : this(node, new SharedState())
    {
    }

    private CommandTreeDefinition(TreeNode<Type> node, SharedState state)
    {
        this.node = node;
        this.state = state;
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
        children?.Invoke(new CommandTreeDefinition(childNode, state));
        return this;
    }

    internal void Close() => state.IsClosed = true;

    private sealed class SharedState
    {
        public HashSet<Type> Types { get; } = new();
        public bool IsClosed { get; set; }
    }
}
