using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Pipes;
using System.Text;

namespace CyberInternauts.CmdStarter.Repl.Remote;

/// <summary>
/// Connects to a <see cref="ReplPipeServer"/> by its pipe name, sends lines and receives their output, errors and exit code.
/// </summary>
public sealed class ReplPipeClient : IAsyncDisposable
{
    private readonly string pipeName;
    private NamedPipeClientStream? pipe;
    private StreamReader? reader;
    private StreamWriter? writer;

    /// <summary>Creates a client for a pipe name.</summary>
    /// <param name="pipeName">The name of the pipe of the server.</param>
    /// <exception cref="ArgumentException"><paramref name="pipeName"/> is null, empty or blank.</exception>
    public ReplPipeClient(string pipeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        this.pipeName = pipeName;
    }

    /// <summary>
    /// Connects to the server on this machine. The server accepts one session at a time and may be between two sessions: this waits for a free server up to the timeout,
    /// and completes when a server has accepted the session and announced it.
    /// </summary>
    /// <param name="timeout">
    /// How long to wait for a server that announces the session, or <see cref="Timeout.InfiniteTimeSpan"/> to wait without limit.
    /// The wait includes the time the session factory of the server takes to create the session.
    /// </param>
    /// <param name="cancellationToken">Token that cancels the wait.</param>
    /// <returns>A task that completes when the session is open.</returns>
    /// <exception cref="TimeoutException">No server announced the session before the timeout.</exception>
    /// <exception cref="InvalidOperationException">This client is already connected.</exception>
    /// <exception cref="InvalidDataException">The server announced the session with a line that the protocol does not know.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is negative and is not <see cref="Timeout.InfiniteTimeSpan"/>.</exception>
    public async Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The timeout cannot be negative, except Timeout.InfiniteTimeSpan.");
        }

        if (pipe is not null)
        {
            throw new InvalidOperationException("The client is already connected.");
        }

        var stopwatch = Stopwatch.StartNew();
        var encoding = new UTF8Encoding(false);
        while (true)
        {
            var stream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            StreamReader? announced = null;
            var connected = false;
            try
            {
                var remaining = Remaining(timeout, stopwatch);
                await stream.ConnectAsync(remaining == Timeout.InfiniteTimeSpan ? Timeout.Infinite : (int)remaining.TotalMilliseconds, cancellationToken);
                announced = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);

                string? line;
                try
                {
                    line = await announced.ReadLineAsync(cancellationToken).AsTask().WaitAsync(Remaining(timeout, stopwatch), cancellationToken);
                }
                catch (IOException)
                {
                    // A connection that waited in the queue of the pipe and was cut when the running session ended: where the system reports it as an error
                    // (a reset connection) instead of the end of the stream, it is treated as the end of the stream.
                    line = null;
                }

                if (line is not null)
                {
                    if (line != ReplPipeServer.Announcement)
                    {
                        throw new InvalidDataException($"Unexpected line from the server: '{line}'.");
                    }

                    pipe = stream;
                    reader = announced;
                    writer = new StreamWriter(stream, encoding, bufferSize: 1024, leaveOpen: true) { NewLine = "\n" };
                    connected = true;
                    return;
                }

                // The connection closed before the announcement: the loop tries again with what is left of the timeout.
            }
            finally
            {
                if (!connected)
                {
                    announced?.Dispose();
                    await stream.DisposeAsync();
                }
            }
        }
    }

    // What is left of the timeout, between zero and what a wait accepts. A wait of zero fails at once with TimeoutException.
    private static TimeSpan Remaining(TimeSpan timeout, Stopwatch stopwatch) =>
        timeout == Timeout.InfiniteTimeSpan
            ? Timeout.InfiniteTimeSpan
            : TimeSpan.FromMilliseconds(Math.Clamp((timeout - stopwatch.Elapsed).TotalMilliseconds, 0, int.MaxValue));

    /// <summary>
    /// Sends a line, copies the lines of output and of errors that the server sends back (without their prefix), and returns the exit code of the line.
    /// A blank line is not sent: the server does not answer it, and this returns 0.
    /// </summary>
    /// <param name="line">The line to run, on one line of text.</param>
    /// <param name="output">Receives the output of the line.</param>
    /// <param name="error">Receives the errors of the line.</param>
    /// <param name="cancellationToken">Token that cancels the wait for the answer.</param>
    /// <returns>The exit code of the line.</returns>
    /// <exception cref="ArgumentException"><paramref name="line"/> contains a line break.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="IOException">The session was closed before the answer ended.</exception>
    /// <exception cref="InvalidDataException">The server sent a line that the protocol does not know.</exception>
    public async Task<int> ExecuteAsync(string line, TextWriter output, TextWriter error, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        if (line.Contains('\n', StringComparison.Ordinal) || line.Contains('\r', StringComparison.Ordinal))
        {
            throw new ArgumentException("A line to run cannot contain a line break.", nameof(line));
        }

        if (reader is null || writer is null)
        {
            throw new InvalidOperationException("The client is not connected.");
        }

        if (string.IsNullOrWhiteSpace(line))
        {
            return 0;
        }

        await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
        await writer.FlushAsync(cancellationToken);

        while (true)
        {
            var response = await reader.ReadLineAsync(cancellationToken)
                ?? throw new IOException("The remote session closed before the answer ended.");
            if (response.StartsWith("O ", StringComparison.Ordinal))
            {
                await output.WriteLineAsync(response.AsMemory(2), cancellationToken);
            }
            else if (response.StartsWith("E ", StringComparison.Ordinal))
            {
                await error.WriteLineAsync(response.AsMemory(2), cancellationToken);
            }
            else if (response.StartsWith("X ", StringComparison.Ordinal) && int.TryParse(response.AsSpan(2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var exitCode))
            {
                return exitCode;
            }
            else
            {
                throw new InvalidDataException($"Unexpected line from the server: '{response}'.");
            }
        }
    }

    /// <summary>
    /// Writes the arguments of a process as one line of REPL, sends it, copies the lines of output and of errors that the server sends back,
    /// and returns the exit code of the line. See <see cref="JoinArguments(IReadOnlyList{string})"/> for how the line is written.
    /// The caller passes the arguments without quotes: the ones that need quotes get them here, so an operator in an argument is never read as one
    /// and the arguments never form a sequence or a pipe. The line is still a line of REPL for the server: a first argument that is a built-in word
    /// of the session (<see cref="ReplStarter.ExitCommands"/>, <see cref="ReplStarter.HelpCommands"/>) keeps its meaning in the REPL.
    /// An empty list gives a blank line, which is not sent and gives 0.
    /// </summary>
    /// <param name="arguments">The arguments, as a process receives them.</param>
    /// <param name="output">Receives the output of the line.</param>
    /// <param name="error">Receives the errors of the line.</param>
    /// <param name="cancellationToken">Token that cancels the wait for the answer.</param>
    /// <returns>The exit code of the line.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="arguments"/>, <paramref name="output"/> or <paramref name="error"/> is null.</exception>
    /// <exception cref="ArgumentException">An argument is null, or contains a double quote or a line break, which a line cannot hold.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="IOException">The session was closed before the answer ended.</exception>
    /// <exception cref="InvalidDataException">The server sent a line that the protocol does not know.</exception>
    public Task<int> ExecuteAsync(IReadOnlyList<string> arguments, TextWriter output, TextWriter error, CancellationToken cancellationToken = default)
        => ExecuteAsync(JoinArguments(arguments), output, error, cancellationToken);

    /// <summary>
    /// Writes the arguments of a process as one line of REPL: the arguments are joined by one space, and an argument is surrounded by double quotes
    /// when it is empty, when one of its characters is white space (<see cref="char.IsWhiteSpace(char)"/>), or when it contains <c>;</c>, <c>&amp;</c> or <c>|</c>.
    /// Any other argument is written as it is. An empty list gives an empty line.
    /// </summary>
    /// <param name="arguments">The arguments, as a process receives them.</param>
    /// <returns>The line.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="arguments"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// An argument is null, or contains a double quote or a line break: neither the reading of the sequences nor the splitting of the line has an escape,
    /// so such an argument cannot be written on a line.
    /// </exception>
    public static string JoinArguments(IReadOnlyList<string> arguments)
    {
        if (!TryJoinArguments(arguments, out var line))
        {
            throw new ArgumentException("An argument that contains a double quote or a line break cannot be written on a line.", nameof(arguments));
        }

        return line;
    }

    // False when an argument cannot be written on a line (a double quote or a line break); a null list or a null argument is a mistake of the caller and throws.
    internal static bool TryJoinArguments(IReadOnlyList<string> arguments, [NotNullWhen(true)] out string? line)
    {
        ThrowIfAnyNull(arguments);

        var builder = new StringBuilder();
        foreach (var argument in arguments)
        {
            if (argument.AsSpan().IndexOfAny('"', '\r', '\n') >= 0)
            {
                line = null;
                return false;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            if (NeedsQuotes(argument))
            {
                builder.Append('"').Append(argument).Append('"');
            }
            else
            {
                builder.Append(argument);
            }
        }

        line = builder.ToString();
        return true;
    }

    // A null list or a null argument is a mistake of the caller, found before anything is read or written.
    private static void ThrowIfAnyNull(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        for (var index = 0; index < arguments.Count; index++)
        {
            if (arguments[index] is null)
            {
                throw new ArgumentException($"The argument at index {index} is null.", nameof(arguments));
            }
        }
    }

    /// <summary>
    /// Runs the arguments of a process on a server that is already running (see <see cref="ReplPipeServer"/>), and returns its exit code,
    /// or returns <see langword="null"/> when the caller has to run them itself. The arguments become one line of REPL as <see cref="JoinArguments(IReadOnlyList{string})"/> writes it,
    /// except for the sequence option below. The output and the errors of the line are copied to the console.
    /// The usual call is <c>return await ReplPipeClient.TryForwardAsync(pipeName, args, TimeSpan.FromMilliseconds(500), CommandConsole.Standard, starter.SequenceOption) ?? await starter.Start(args);</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The result is <see langword="null"/>, with nothing sent, when no server accepted the session before the timeout (the client does not tell an absent server from a busy one,
    /// which accepts one session at a time), when <paramref name="arguments"/> is empty, and when an argument contains a double quote or a line break: such an argument cannot be written on a line.
    /// </para>
    /// <para>
    /// <paramref name="sequenceOption"/> follows the same rule as <see cref="Starter.Start(string[])"/> with <see cref="Starter.SequenceOption"/>:
    /// when it is not <see langword="null"/> and <paramref name="arguments"/> is exactly that option followed by one line, the line is sent as it is, because a line of REPL is already a sequence.
    /// Such a line may contain double quotes. It gives <see langword="null"/>, with nothing sent, when it is blank or contains a line break.
    /// </para>
    /// <para>
    /// Without any server, the connection is retried until the timeout: every local fallback costs the whole timeout, so choose a short one.
    /// </para>
    /// <para>
    /// Once the session is open, the command may have run on the server: a failure from then on is not turned into <see langword="null"/>, because the application must not run the command a second time.
    /// </para>
    /// </remarks>
    /// <param name="pipeName">The name of the pipe of the server. A pipe name is shared by all the users of the machine: it should contain the name of the user.</param>
    /// <param name="arguments">The arguments, as a process receives them.</param>
    /// <param name="timeout">How long to wait for a server that accepts the session.</param>
    /// <param name="console">Receives the output and the errors of the line. Its input is not sent: a remote command reads no standard input.</param>
    /// <param name="sequenceOption">The value of <see cref="Starter.SequenceOption"/> of the application, or <see langword="null"/> when it has none.</param>
    /// <param name="cancellationToken">Token that cancels the wait for the server and for the answer.</param>
    /// <returns>The exit code of the line, or <see langword="null"/> when nothing was sent and the caller has to run the arguments itself.</returns>
    /// <exception cref="ArgumentException"><paramref name="pipeName"/> is null, empty or blank, or an argument is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="arguments"/> or <paramref name="console"/> is null.</exception>
    /// <exception cref="IOException">The session was closed before the answer ended.</exception>
    /// <exception cref="InvalidDataException">The server sent a line that the protocol does not know.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    /// <exception cref="UnauthorizedAccessException">A pipe of this name belongs to another user.</exception>
    public static async Task<int?> TryForwardAsync(string pipeName, IReadOnlyList<string> arguments, TimeSpan timeout, CommandConsole console, string? sequenceOption = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(console);
        ThrowIfAnyNull(arguments);

        string line;
        if (sequenceOption is not null && arguments.Count == 2 && string.Equals(arguments[0], sequenceOption, StringComparison.Ordinal))
        {
            // A line of REPL may hold double quotes, so it does not go through TryJoinArguments.
            line = arguments[1];
            if (string.IsNullOrWhiteSpace(line) || line.Contains('\r', StringComparison.Ordinal) || line.Contains('\n', StringComparison.Ordinal))
            {
                return null;
            }
        }
        else
        {
            if (arguments.Count == 0 || !TryJoinArguments(arguments, out var joined))
            {
                return null;
            }

            line = joined;
        }

        await using var client = new ReplPipeClient(pipeName);
        try
        {
            await client.ConnectAsync(timeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            return null;
        }

        return await client.ExecuteAsync(line, console.Out, console.Error, cancellationToken);
    }

    private static bool NeedsQuotes(string argument) => argument.Length == 0 || argument.Any(c => char.IsWhiteSpace(c) || c is ';' or '&' or '|');

    /// <summary>
    /// Writes a prompt, reads a line from the input, runs it on the server and starts again, until the end of the input, until the server closes the session
    /// (reading reaches the end of the stream or writing is refused: no exception is raised), or after a line that, trimmed, is <c>exit</c> or <c>quit</c>
    /// (case ignored). The client does not know the exit words of the server: it assumes these two.
    /// </summary>
    /// <param name="input">Where the lines come from.</param>
    /// <param name="output">Receives the prompt and the output of the lines.</param>
    /// <param name="error">Receives the errors of the lines.</param>
    /// <param name="prompt">The text written before each line.</param>
    /// <param name="cancellationToken">Token that cancels the session.</param>
    /// <returns>The code of the last line that was run, 0 if none.</returns>
    public async Task<int> RunInteractiveAsync(TextReader input, TextWriter output, TextWriter error, string prompt = "> ", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(prompt);

        var lastCode = 0;
        while (true)
        {
            await output.WriteAsync(prompt.AsMemory(), cancellationToken);
            await output.FlushAsync(cancellationToken);
            var line = await input.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                lastCode = await ExecuteAsync(line, output, error, cancellationToken);
            }
            catch (IOException)
            {
                // The server closed the session: the interactive loop ends.
                break;
            }

            var trimmed = line.Trim();
            if (string.Equals(trimmed, "exit", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "quit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }

        return lastCode;
    }

    /// <summary>Closes the session.</summary>
    /// <returns>A task that completes when the pipe is closed.</returns>
    public async ValueTask DisposeAsync()
    {
        var currentWriter = writer;
        var currentReader = reader;
        var currentPipe = pipe;
        writer = null;
        reader = null;
        pipe = null;

        if (currentWriter is not null)
        {
            try
            {
                await currentWriter.DisposeAsync();
            }
            catch (IOException)
            {
                // The server already closed the pipe: there is nothing left to flush.
            }
        }

        currentReader?.Dispose();
        if (currentPipe is not null)
        {
            await currentPipe.DisposeAsync();
        }
    }
}
