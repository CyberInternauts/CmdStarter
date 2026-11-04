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

    /// <summary>Connects to the server on this machine. The server accepts one session at a time and may be between two sessions: this waits for a free server up to the timeout.</summary>
    /// <param name="timeout">How long to wait for a server that accepts the session.</param>
    /// <param name="cancellationToken">Token that cancels the wait.</param>
    /// <returns>A task that completes when the session is open.</returns>
    /// <exception cref="TimeoutException">No server accepted the session before the timeout.</exception>
    /// <exception cref="InvalidOperationException">This client is already connected.</exception>
    public async Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (pipe is not null)
        {
            throw new InvalidOperationException("The client is already connected.");
        }

        var milliseconds = timeout == Timeout.InfiniteTimeSpan ? Timeout.Infinite : (int)Math.Min(int.MaxValue, timeout.TotalMilliseconds);
        var stream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        var connected = false;
        try
        {
            await stream.ConnectAsync(milliseconds, cancellationToken);
            connected = true;
        }
        finally
        {
            if (!connected)
            {
                await stream.DisposeAsync();
            }
        }

        var encoding = new UTF8Encoding(false);
        pipe = stream;
        reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        writer = new StreamWriter(stream, encoding, bufferSize: 1024, leaveOpen: true) { NewLine = "\n" };
    }

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
