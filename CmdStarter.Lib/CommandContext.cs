using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;

namespace CyberInternauts.CmdStarter;

/// <summary>Describes a command execution after its inputs have been bound.</summary>
public sealed class CommandContext
{
    internal CommandContext(Command command, string commandPath, MemberInfo member, object? instance, ParseResult parseResult, IServiceProvider? services, CancellationToken cancellationToken)
    {
        Command = command;
        CommandPath = commandPath;
        Member = member;
        Instance = instance;
        ParseResult = parseResult;
        Services = services;
        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the System.CommandLine command being executed.</summary>
    public Command Command { get; }

    /// <summary>Gets the command path, excluding the root command.</summary>
    public string CommandPath { get; }

    /// <summary>Gets the command type for class commands or the attributed method for method commands.</summary>
    public MemberInfo Member { get; }

    /// <summary>Gets the bound command instance, or the declaring type instance for method commands; static methods have no instance.</summary>
    public object? Instance { get; }

    /// <summary>Gets the parse result containing the bound command line values.</summary>
    public ParseResult ParseResult { get; }

    /// <summary>Gets the execution scope's service provider, or <see langword="null"/> when dependency injection is not configured.</summary>
    public IServiceProvider? Services { get; }

    /// <summary>Gets the cancellation token for this execution.</summary>
    public CancellationToken CancellationToken { get; }
}
