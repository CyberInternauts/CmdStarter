using System.CommandLine;
using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter;

/// <summary>Provides details about a command handler that completed successfully.</summary>
public sealed class CommandHandledEventArgs : EventArgs
{
    /// <summary>Creates event data for a completed command handler.</summary>
    public CommandHandledEventArgs(Command command, object? instance, int exitCode)
    {
        Command = command;
        Instance = instance;
        ExitCode = exitCode;
    }

    /// <summary>Gets the System.CommandLine node that was executed.</summary>
    public Command Command { get; }

    /// <summary>Gets the object that ran the handler: the command instance for the class-based model, the class instance for an instance method, or <see langword="null"/> for a static method.</summary>
    public object? Instance { get; }

    /// <summary>Gets the handler's exit code.</summary>
    public int ExitCode { get; }
}
