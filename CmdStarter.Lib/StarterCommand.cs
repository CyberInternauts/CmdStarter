using com.cyberinternauts.csharp.CmdStarter.Lib.Extensions;
using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using static com.cyberinternauts.csharp.CmdStarter.Lib.Reflection.Helper;

namespace com.cyberinternauts.csharp.CmdStarter.Lib;

/// <summary>
/// Base class for commands handled by CmdStarter.
/// </summary>
[AllOptionsExcluded]
public abstract class StarterCommand : IStarterCommand
{
    /// <summary>
    /// Options prefix.
    /// </summary>
    public const string OPTION_PREFIX = "--";

    /// <summary>
    /// Description joiner upon multiple attributes.
    /// </summary>
    public const string DESCRIPTION_JOINER = "\n";

    /// <inheritdoc cref="IStarterCommand.HandlingMethod"/>
    public virtual Delegate HandlingMethod { get; } = IStarterCommand.EMPTY_EXECUTION;

    /// <inheritdoc cref="IStarterCommand.GlobalOptionsManager"/>
    public virtual GlobalOptionsManager? GlobalOptionsManager { get; set; }

    /// <summary>
    /// Gets the command name used in the command tree.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description supplied to the constructor.
    /// </summary>
    public string? Description { get; }

    internal System.CommandLine.Command? CliCommand { get; set; }

    /// <summary>
    /// Constructor using its own type as command name.
    /// </summary>
    protected StarterCommand()
    {
        Name = GetType().Name.PascalToKebabCase();
    }

    /// <summary>
    /// Constructor with name and potentially description.
    /// </summary>
    /// <param name="name">Name of the command.</param>
    /// <param name="description">Description of the command.</param>
    /// <exception cref="ArgumentException">Name can't be null, empty or only whitespace.</exception>
    protected StarterCommand(string name, string? description = null)
    {
        if (String.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(nameof(name) + " parameter can't be null or white spaces");
        }

        Name = name.ToCommandName();
        Description = description;
    }

    /// <summary>
    /// Shortcut method for <see cref="IStarterCommandExtensions.GetGlobalOptions{GlobalOptionsType}(IStarterCommand)"/>.
    /// </summary>
    /// <typeparam name="GlobalOptionsType">Global options type.</typeparam>
    /// <returns>The global options object, or <see langword="null"/> when unavailable.</returns>
    public GlobalOptionsType? GetGlobalOptions<GlobalOptionsType>() where GlobalOptionsType : class, IGlobalOptionsContainer
    {
        return ((IStarterCommand)this).GetGlobalOptions<GlobalOptionsType>();
    }

    /// <summary>
    /// Gets the full command path, excluding the root command.
    /// </summary>
    /// <returns>The command path, or an empty string when the tree is not built.</returns>
    public string GetFullCommandString()
    {
        return CliCommand is null ? string.Empty : Starter.GetCommandPath(CliCommand);
    }

    /// <summary>
    /// Gets an instance of a type implementing <see cref="IStarterCommand"/>.
    /// </summary>
    /// <typeparam name="CommandType">Command type to instantiate.</typeparam>
    /// <returns>An instance of the requested command type.</returns>
    public static IStarterCommand GetInstance<CommandType>() where CommandType : IStarterCommand
    {
        var method = FindGetInstanceMethod(typeof(CommandType));
        return (IStarterCommand)method.Invoke(null, null)!;
    }
}
