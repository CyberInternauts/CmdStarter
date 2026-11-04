using System.Collections.Immutable;
using System.CommandLine;
using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Repl;

/// <summary>
/// Main class executing the command using the command line arguments in REPL mode.
/// </summary>
public sealed class ReplStarter : Starter
{
    private readonly IReplInputProvider inputProvider;
    private readonly object commandLock = new();
    private CancellationTokenSource? currentCommand;
    private bool allowReplLoop = true;

    /// <summary>Gets invoked after the execution of a command.</summary>
    public event EventHandler<ReplCommandEventArgs>? OnCommandExecuted;

    /// <summary>Gets or sets the case-insensitive commands that end the loop; an empty list disables them.</summary>
    public ImmutableList<string> ExitCommands { get; set; } = ImmutableList.Create("exit", "quit");

    /// <summary>Gets or sets the case-insensitive commands that show help; an empty list disables them.</summary>
    public ImmutableList<string> HelpCommands { get; set; } = ImmutableList.Create("help");

    /// <summary>Constructor without namespace filters.</summary>
    /// <param name="inputProvider">Provides input for command parsing.</param>
    public ReplStarter(IReplInputProvider inputProvider) : base()
    {
        this.inputProvider = inputProvider;
    }

    /// <summary>Constructor with namespace filters.</summary>
    /// <param name="inputProvider">Provides input for command parsing.</param>
    /// <param name="namespaces">Array of included namespaces.</param>
    public ReplStarter(IReplInputProvider inputProvider, string[] namespaces) : base(namespaces)
    {
        this.inputProvider = inputProvider;
    }

    /// <summary>Constructor with namespace filters.</summary>
    /// <param name="inputProvider">Provides input for command parsing.</param>
    /// <param name="namespaces">List of included namespaces.</param>
    public ReplStarter(IReplInputProvider inputProvider, List<string> namespaces) : base(namespaces)
    {
        this.inputProvider = inputProvider;
    }

    /// <summary>Runs the first command and starts the REPL loop.</summary>
    /// <param name="args">Arguments for the first command.</param>
    /// <param name="cancellationToken">Token used to stop the loop and cancel commands.</param>
    public Task Launch(string[] args, CancellationToken cancellationToken = default) =>
        LaunchCoreAsync(() => ExecuteCommandAsync(args, cancellationToken), cancellationToken);

    /// <summary>Runs the first command and starts the REPL loop.</summary>
    /// <param name="args">Arguments for the first command.</param>
    /// <param name="cancellationToken">Token used to stop the loop and cancel commands.</param>
    public Task Launch(string args, CancellationToken cancellationToken = default) =>
        LaunchCoreAsync(() => ExecuteCommandAsync(args, cancellationToken), cancellationToken);

    /// <summary>Starts the REPL input loop.</summary>
    /// <param name="cancellationToken">Token used to stop the loop and cancel commands.</param>
    public Task Launch(CancellationToken cancellationToken = default) => LaunchCoreAsync(null, cancellationToken);

    private async Task LaunchCoreAsync(Func<Task>? initialCommand, CancellationToken cancellationToken)
    {
        allowReplLoop = true;
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            if (CancelCurrentCommand())
            {
                eventArgs.Cancel = true;
            }
        };
        Console.CancelKeyPress += cancelHandler;
        try
        {
            if (initialCommand is not null)
            {
                await initialCommand();
            }

            while (allowReplLoop && !cancellationToken.IsCancellationRequested)
            {
                string? input;
                try
                {
                    input = await inputProvider.GetInputAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (input is null)
                {
                    break;
                }

                var line = input.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (ExitCommands.Any(command => string.Equals(line, command, StringComparison.OrdinalIgnoreCase)))
                {
                    break;
                }

                var firstSpace = FindWhitespace(line);
                var firstWord = firstSpace < 0 ? line : line[..firstSpace];
                var rest = firstSpace < 0 ? string.Empty : line[(firstSpace + 1)..].Trim();
                if (HelpCommands.Any(command => string.Equals(firstWord, command, StringComparison.OrdinalIgnoreCase)))
                {
                    await ExecuteCommandAsync(rest.Length == 0 ? "--help" : $"{rest} --help", cancellationToken);
                    continue;
                }

                await ExecuteCommandAsync(line, cancellationToken);
            }
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    /// <summary>Stops the REPL loop.</summary>
    public void Stop()
    {
        allowReplLoop = false;
    }

    /// <summary>Cancels the current command, returning whether cancellation was requested.</summary>
    public bool CancelCurrentCommand()
    {
        lock (commandLock)
        {
            if (currentCommand is null || currentCommand.IsCancellationRequested)
            {
                return false;
            }

            currentCommand.Cancel();
            return true;
        }
    }

    /// <inheritdoc cref="Starter.Start(string[])"/>
    /// <remarks>This raises the <see cref="OnCommandExecuted"/> event.</remarks>
    public new Task<int> Start(string[] args) => Start(args, CancellationToken.None);

    /// <inheritdoc cref="Starter.Start(string)"/>
    /// <remarks>This raises the <see cref="OnCommandExecuted"/> event.</remarks>
    public new Task<int> Start(string args) => Start(args, CancellationToken.None);

    /// <summary>Executes arguments and raises <see cref="OnCommandExecuted"/>.</summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="cancellationToken">Token used to cancel command execution.</param>
    /// <returns>The command exit code.</returns>
    public new Task<int> Start(string[] args, CancellationToken cancellationToken) => ExecuteAndRaiseAsync(args, cancellationToken);

    /// <summary>Executes arguments and raises <see cref="OnCommandExecuted"/>.</summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="cancellationToken">Token used to cancel command execution.</param>
    /// <returns>The command exit code.</returns>
    public new Task<int> Start(string args, CancellationToken cancellationToken) => ExecuteAndRaiseAsync(args, cancellationToken);

    /// <inheritdoc/>
    protected override InvocationConfiguration? CreateInvocationConfiguration() => new()
    {
        ProcessTerminationTimeout = null
    };

    private async Task ExecuteCommandAsync(string args, CancellationToken cancellationToken)
    {
        _ = await ExecuteAndRaiseAsync(args, cancellationToken);
    }

    private async Task ExecuteCommandAsync(string[] args, CancellationToken cancellationToken)
    {
        _ = await ExecuteAndRaiseAsync(args, cancellationToken);
    }

    private async Task<int> ExecuteAndRaiseAsync(string args, CancellationToken cancellationToken)
    {
        using var commandCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lock (commandLock)
        {
            currentCommand = commandCancellation;
        }

        try
        {
            return await RaiseCommandExecutedAsync(await base.Start(args, commandCancellation.Token));
        }
        finally
        {
            lock (commandLock)
            {
                if (ReferenceEquals(currentCommand, commandCancellation))
                {
                    currentCommand = null;
                }
            }
        }
    }

    private async Task<int> ExecuteAndRaiseAsync(string[] args, CancellationToken cancellationToken)
    {
        using var commandCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lock (commandLock)
        {
            currentCommand = commandCancellation;
        }

        try
        {
            return await RaiseCommandExecutedAsync(await base.Start(args, commandCancellation.Token));
        }
        finally
        {
            lock (commandLock)
            {
                if (ReferenceEquals(currentCommand, commandCancellation))
                {
                    currentCommand = null;
                }
            }
        }
    }

    private Task<int> RaiseCommandExecutedAsync(int returnCode)
    {
        OnCommandExecuted?.Invoke(this, new ReplCommandEventArgs(returnCode));
        return Task.FromResult(returnCode);
    }

    private static int FindWhitespace(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (char.IsWhiteSpace(value[index]))
            {
                return index;
            }
        }

        return -1;
    }
}
