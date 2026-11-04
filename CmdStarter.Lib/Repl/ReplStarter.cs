using System.Collections.Immutable;
using System.CommandLine;
using CyberInternauts.CmdStarter.Interfaces;
using CyberInternauts.CmdStarter.Sequences;

namespace CyberInternauts.CmdStarter.Repl;

/// <summary>
/// Main class executing the command using the command line arguments in REPL mode.
/// </summary>
public sealed class ReplStarter : Starter
{
    private readonly IReplInputProvider inputProvider;
    private readonly Lock commandLock = new();
    private CancellationTokenSource? currentCommand;
    private bool allowReplLoop = true;
    private volatile bool isLaunched;

    /// <summary>Gets invoked after the execution of a command.</summary>
    public event EventHandler<ReplCommandEventArgs>? OnCommandExecuted;

    /// <summary>Gets or sets the case-insensitive commands that end the loop; an empty list disables them.</summary>
    public ImmutableList<string> ExitCommands { get; set; } = ImmutableList.Create("exit", "quit");

    /// <summary>Gets or sets the case-insensitive commands that show help; an empty list disables them.</summary>
    public ImmutableList<string> HelpCommands { get; set; } = ["help"];

    /// <summary>Constructor without namespace filters.</summary>
    /// <param name="inputProvider">Provides input for command parsing.</param>
    public ReplStarter(IReplInputProvider inputProvider) : base()
    {
        this.inputProvider = inputProvider;
        ShareCompletions();
    }

    /// <summary>Constructor with namespace filters.</summary>
    /// <param name="inputProvider">Provides input for command parsing.</param>
    /// <param name="namespaces">Array of included namespaces.</param>
    public ReplStarter(IReplInputProvider inputProvider, string[] namespaces) : base(namespaces)
    {
        this.inputProvider = inputProvider;
        ShareCompletions();
    }

    /// <summary>Constructor with namespace filters.</summary>
    /// <param name="inputProvider">Provides input for command parsing.</param>
    /// <param name="namespaces">List of included namespaces.</param>
    public ReplStarter(IReplInputProvider inputProvider, List<string> namespaces) : base(namespaces)
    {
        this.inputProvider = inputProvider;
        ShareCompletions();
    }

    /// <summary>Runs the first command and starts the REPL loop.</summary>
    /// <param name="args">Arguments for the first command.</param>
    /// <param name="cancellationToken">Token used to stop the loop and cancel commands.</param>
    public Task Launch(string[] args, CancellationToken cancellationToken = default) =>
        LaunchCoreAsync(() => Start(args, cancellationToken), cancellationToken);

    /// <summary>Runs the first command and starts the REPL loop.</summary>
    /// <param name="args">Arguments for the first command.</param>
    /// <param name="cancellationToken">Token used to stop the loop and cancel commands.</param>
    public Task Launch(string args, CancellationToken cancellationToken = default) =>
        LaunchCoreAsync(() => Start(args, cancellationToken), cancellationToken);

    /// <summary>Starts the REPL input loop.</summary>
    /// <param name="cancellationToken">Token used to stop the loop and cancel commands.</param>
    public Task Launch(CancellationToken cancellationToken = default) => LaunchCoreAsync(null, cancellationToken);

    // True while a Launch loop runs: a remote server refuses such a starter as a session (it would stop the local loop and take its console).
    internal bool IsLaunched => isLaunched;

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
        isLaunched = true;
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

                await ExecuteLineAsync(input, cancellationToken);
            }
        }
        finally
        {
            isLaunched = false;
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    // Executes one line of the REPL: trimming, blank line, sequence, pipes, built-in words. The loop of Launch and the remote sessions share it.
    // ExitCode is null when there is nothing to answer (a blank line); EndsSession tells that an exit word (or Stop) ended the session.
    internal async Task<ReplLineResult> ExecuteLineAsync(string input, CancellationToken cancellationToken)
    {
        var line = input.Trim();
        if (line.Length == 0)
        {
            return new ReplLineResult(null, !allowReplLoop);
        }

        int? exitCode;
        if (!CommandSequence.TryParse(line, out var sequence, out var error))
        {
            if (CommandSequence.HasOperator(line))
            {
                (DefaultConsole?.Error ?? Console.Error).WriteLine(error);
                exitCode = 1;
            }
            else
            {
                // A line without operator behaves as before (an unclosed quote is left to System.CommandLine).
                exitCode = await RunReplStepAsync(line, null, cancellationToken) ?? 0;
            }
        }
        else if (sequence.Steps.Count == 0)
        {
            exitCode = await Start(line, cancellationToken);
        }
        else
        {
            exitCode = await sequence.ExecuteAsync(step => RunReplPipelineAsync(step, cancellationToken));
        }

        return new ReplLineResult(exitCode, !allowReplLoop);
    }

    // Runs one step of a REPL line, a pipeline of commands. A built-in exit word cannot be part of a pipeline of several commands:
    // nothing of the pipeline runs and the step returns 1.
    private async Task<int?> RunReplPipelineAsync(CommandSequenceStep step, CancellationToken cancellationToken)
    {
        if (step.Pipeline.Count > 1)
        {
            var exitWord = step.Pipeline.FirstOrDefault(IsExitWord);
            if (exitWord is not null)
            {
                (DefaultConsole?.Error ?? Console.Error).WriteLine($"Command '{exitWord}' cannot be piped.");
                return 1;
            }
        }

        return await RunPipelineAsync(step.Pipeline, null, (command, console) => RunReplStepAsync(command, console, cancellationToken));
    }

    // Runs one command of a REPL line: the built-in words keep their meaning inside a sequence.
    // Returns null to stop the sequence when the command ends the loop. A null console means a plain Start.
    private async Task<int?> RunReplStepAsync(string command, CommandConsole? console, CancellationToken cancellationToken)
    {
        if (IsExitWord(command))
        {
            allowReplLoop = false;
            return null;
        }

        var firstSpace = FindWhitespace(command);
        var firstWord = firstSpace < 0 ? command : command[..firstSpace];
        var rest = firstSpace < 0 ? string.Empty : command[(firstSpace + 1)..].Trim();
        var commandLine = IsHelpWord(firstWord) ? (rest.Length == 0 ? "--help" : $"{rest} --help") : command;
        return console is null
            ? await Start(commandLine, cancellationToken)
            : await Start(commandLine, console, cancellationToken);
    }

    private bool IsExitWord(string command) =>
        ExitCommands.Any(exitCommand => string.Equals(command, exitCommand, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets the completion candidates for a line at a cursor position: the visible commands in the current state,
    /// their options and <c>[AutoComplete]</c> values, plus the built-in words (<see cref="ExitCommands"/>, <see cref="HelpCommands"/>)
    /// when the cursor is in the first word. After a help word, the rest of the line is completed as a command line.
    /// </summary>
    /// <param name="line">The line being edited.</param>
    /// <param name="position">The cursor position in <paramref name="line"/>.</param>
    /// <returns>The distinct candidates, sorted ordinally.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="line"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is negative or beyond the end of <paramref name="line"/>.</exception>
    public IReadOnlyList<string> GetCompletions(string line, int position)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(position, line.Length);

        InstantiateCommands();
        UpdateCommandAvailability();

        // In a sequence, only the step that contains the cursor is completed (the whole line when the cursor is inside an operator).
        if (CommandSequence.FindStep(line, position) is { } step)
        {
            var segment = line[step.Start..step.End];
            var leadingBlanks = segment.Length - segment.TrimStart().Length;
            line = segment[leadingBlanks..];
            position = Math.Max(0, position - step.Start - leadingBlanks);
        }

        var firstSpace = FindWhitespace(line);
        if (firstSpace >= 0 && position > firstSpace && IsHelpWord(line[..firstSpace]))
        {
            var restStart = firstSpace + 1;
            return GetCommandCompletions(line[restStart..], position - restStart);
        }

        var completions = new HashSet<string>(GetCommandCompletions(line, position), StringComparer.Ordinal);
        var typedWord = line[..position];
        if (FindWhitespace(typedWord) < 0)
        {
            completions.UnionWith(ExitCommands.Concat(HelpCommands).Where(word => word.StartsWith(typedWord, StringComparison.Ordinal)));
        }

        return [.. completions.Order(StringComparer.Ordinal)];
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
    public new Task<int> Start(string[] args, CancellationToken cancellationToken) =>
        IsSequenceInvocation(args)
            ? StartSequence(args[1], cancellationToken)
            : ExecuteAndRaiseAsync(token => base.Start(args, token), cancellationToken);

    /// <summary>Executes arguments and raises <see cref="OnCommandExecuted"/>.</summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="cancellationToken">Token used to cancel command execution.</param>
    /// <returns>The command exit code.</returns>
    public new Task<int> Start(string args, CancellationToken cancellationToken) =>
        ExecuteAndRaiseAsync(token => base.Start(args, token), cancellationToken);

    /// <summary>Executes arguments with the console of this execution and raises <see cref="OnCommandExecuted"/>.</summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="console">The input, output and error of this execution.</param>
    /// <param name="cancellationToken">Token used to cancel command execution.</param>
    /// <returns>The command exit code.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="console"/> is null.</exception>
    public new Task<int> Start(string[] args, CommandConsole console, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(console);
        return IsSequenceInvocation(args)
            ? StartSequence(args[1], console, cancellationToken)
            : ExecuteAndRaiseAsync(token => base.Start(args, console, token), cancellationToken);
    }

    /// <summary>Executes arguments with the console of this execution and raises <see cref="OnCommandExecuted"/>.</summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="console">The input, output and error of this execution.</param>
    /// <param name="cancellationToken">Token used to cancel command execution.</param>
    /// <returns>The command exit code.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="console"/> is null.</exception>
    public new Task<int> Start(string args, CommandConsole console, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(console);
        return ExecuteAndRaiseAsync(token => base.Start(args, console, token), cancellationToken);
    }

    /// <summary>Runs a sequence of commands (see <see cref="Starter.StartSequence(string, CancellationToken)"/>); each executed command raises <see cref="OnCommandExecuted"/>.</summary>
    /// <param name="commandLine">The line to run.</param>
    /// <param name="cancellationToken">Token used to cancel command execution.</param>
    /// <returns>The exit code of the last executed command; 1 when the line is not a valid sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="commandLine"/> is null.</exception>
    public new Task<int> StartSequence(string commandLine, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commandLine);
        return RunSequenceAsync(commandLine, null, (command, commandConsole) => commandConsole is null ? Start(command, cancellationToken) : Start(command, commandConsole, cancellationToken));
    }

    /// <summary>Runs a sequence of commands with the given console (see <see cref="Starter.StartSequence(string, CommandConsole, CancellationToken)"/>); each executed command raises <see cref="OnCommandExecuted"/>.</summary>
    /// <param name="commandLine">The line to run.</param>
    /// <param name="console">The input, output and error of this execution, also used to report an invalid line.</param>
    /// <param name="cancellationToken">Token used to cancel command execution.</param>
    /// <returns>The exit code of the last executed command; 1 when the line is not a valid sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="commandLine"/> or <paramref name="console"/> is null.</exception>
    public new Task<int> StartSequence(string commandLine, CommandConsole console, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commandLine);
        ArgumentNullException.ThrowIfNull(console);
        // commandConsole is never null: RunSequenceAsync and RunPipelineAsync pass the console of this overload
        return RunSequenceAsync(commandLine, console, (command, commandConsole) => Start(command, commandConsole!, cancellationToken));
    }

    /// <inheritdoc/>
    protected override InvocationConfiguration? CreateInvocationConfiguration() => new()
    {
        ProcessTerminationTimeout = null
    };

    // Runs one command as the current command (so Ctrl+C can cancel it), then raises OnCommandExecuted.
    private async Task<int> ExecuteAndRaiseAsync(Func<CancellationToken, Task<int>> start, CancellationToken cancellationToken)
    {
        int returnCode;
        using (var commandCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            lock (commandLock)
            {
                currentCommand = commandCancellation;
            }

            try
            {
                returnCode = await start(commandCancellation.Token);
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

        OnCommandExecuted?.Invoke(this, new ReplCommandEventArgs(returnCode));
        return returnCode;
    }

    // Gives the console provider this starter's completions, unless the application already set its own source.
    private void ShareCompletions()
    {
        if (inputProvider is ConsoleReplProvider { CompletionSource: null } consoleProvider)
        {
            consoleProvider.CompletionSource = GetCompletions;
        }
    }

    private bool IsHelpWord(string word) =>
        HelpCommands.Any(command => string.Equals(word, command, StringComparison.OrdinalIgnoreCase));

    private string[] GetCommandCompletions(string line, int position) =>
        [.. RootCommand.Parse(line).GetCompletions(position)
            .Select(item => item.Label)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

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

// The result of one REPL line: its exit code (null when a blank line has nothing to answer) and whether the session ended.
internal readonly record struct ReplLineResult(int? ExitCode, bool EndsSession);
