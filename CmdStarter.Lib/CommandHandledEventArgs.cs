using System.CommandLine;
using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Lib;

/// <summary>Provides details about a command handler that completed successfully.</summary>
public sealed class CommandHandledEventArgs : EventArgs
{
    /// <summary>Creates event data for a completed command handler.</summary>
    public CommandHandledEventArgs(Command command, IStarterCommand instance, int exitCode)
    {
        Command = command;
        Instance = instance;
        ExitCode = exitCode;
    }

    /// <summary>Gets the System.CommandLine node that was executed.</summary>
    public Command Command { get; }

    /// <summary>Gets the command instance that handled the execution.</summary>
    public IStarterCommand Instance { get; }

    /// <summary>Gets the handler's exit code.</summary>
    public int ExitCode { get; }
}
