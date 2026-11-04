using System.Diagnostics.CodeAnalysis;

namespace CyberInternauts.CmdStarter.Interfaces;

/// <summary>Runs code around a command execution.</summary>
public interface ICommandFilter
{
    /// <summary>Invokes this filter around the remaining filters and the command.</summary>
    /// <param name="context">The bound command execution context.</param>
    /// <param name="next">Invoking this delegate runs the next filter and ultimately the command. Omitting the call prevents the command from running.</param>
    /// <returns>The command exit code, which this filter may replace.</returns>
    [SuppressMessage("Naming", "CA1716", Justification = "The next parameter name matches the required command-filter contract and middleware convention.")]
    Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next);
}
