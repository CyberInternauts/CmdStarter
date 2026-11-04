using System.Globalization;
using System.IO.Pipes;
using System.Text;

namespace CyberInternauts.CmdStarter.Repl.Remote;

/// <summary>
/// Serves a REPL over a named pipe, one session at a time. Each session has its own <see cref="ReplStarter"/>, created by the application,
/// so the state of a session (a login, for instance) lives and ends with it. Only the user who runs the server can connect.
/// </summary>
/// <remarks>
/// The protocol is UTF-8 text, one line per message. The client sends a line of the REPL (built-in words, sequences and pipes included);
/// a blank line has no answer. For every other line the server sends lines <c>O text</c> (output) and <c>E text</c> (error), in the order
/// they were written, then a line <c>X code</c> that ends the answer. After <c>exit</c> or <c>quit</c> the answer is <c>X 0</c> and the session is closed.
/// The server never uses <see cref="Console"/>: the console of the process stays available to a local REPL.
/// </remarks>
public sealed class ReplPipeServer
{
    private const string SharedSessionMessage = "The session factory must create a new ReplStarter for each session.";

    private readonly string pipeName;
    private readonly Func<ReplStarter> createSession;
    private WeakReference<ReplStarter>? previousSession;

    /// <summary>Creates a server for a pipe name and a factory of sessions.</summary>
    /// <param name="pipeName">The name of the pipe.</param>
    /// <param name="createSession">
    /// Creates a new <see cref="ReplStarter"/> for each session, with its own state; the server does not dispose what the factory creates.
    /// Under dependency injection, build the session for each call: do not resolve a singleton <see cref="ReplStarter"/>, which would share a login
    /// between sessions and let a remote <c>exit</c> stop the local REPL. Returning the instance of the previous session, or a <see cref="ReplStarter"/>
    /// whose <see cref="ReplStarter.Launch(CancellationToken)"/> loop is running, makes <see cref="RunAsync"/> throw <see cref="InvalidOperationException"/>.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="pipeName"/> is null, empty or blank.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="createSession"/> is null.</exception>
    public ReplPipeServer(string pipeName, Func<ReplStarter> createSession)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentNullException.ThrowIfNull(createSession);
        this.pipeName = pipeName;
        this.createSession = createSession;
    }

    /// <summary>
    /// Accepts sessions one after the other until the token is cancelled. A session ends on <c>exit</c> or <c>quit</c>, or when the client disconnects;
    /// an input or output error of the pipe closes that session only. Cancelling closes the current session and ends this task without exception.
    /// </summary>
    /// <param name="cancellationToken">Token that stops the server.</param>
    /// <returns>A task that completes when the server has stopped.</returns>
    /// <exception cref="InvalidOperationException">The session factory returned the instance of the previous session, or a <see cref="ReplStarter"/> whose local loop is running.</exception>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await RunSessionAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancelling is the normal way to stop the server.
        }
    }

    private async Task RunSessionAsync(CancellationToken cancellationToken)
    {
        await using var pipe = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        try
        {
            await pipe.WaitForConnectionAsync(cancellationToken);
            await ServeAsync(pipe, cancellationToken);
        }
        catch (IOException)
        {
            // The client left in the middle of an exchange: this session ends, the server goes on.
        }
        catch (ObjectDisposedException)
        {
            // Same: the pipe was closed under the session.
        }
    }

    private async Task ServeAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        var encoding = new UTF8Encoding(false);
        using var reader = new StreamReader(pipe, encoding, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        await using var writer = new StreamWriter(pipe, encoding, bufferSize: 1024, leaveOpen: true) { NewLine = "\n" };
        var gate = new object();
        using var output = new PrefixedLineWriter("O ", writer, gate);
        using var error = new PrefixedLineWriter("E ", writer, gate);

        // The session writes to the pipe only: it reads nothing from the console and never touches System.Console.
        var session = createSession();
        // A shared starter would carry the state of one session into the next, or stop and take over a local REPL: it is a configuration error.
        if (session.IsLaunched || (previousSession is not null && previousSession.TryGetTarget(out var previous) && ReferenceEquals(previous, session)))
        {
            throw new InvalidOperationException(SharedSessionMessage);
        }

        previousSession = new WeakReference<ReplStarter>(session);
        session.DefaultConsole = new CommandConsole(TextReader.Null, output, error);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            var result = await session.ExecuteLineAsync(line, cancellationToken);
            if (result.ExitCode is { } exitCode)
            {
                output.FlushPending();
                error.FlushPending();
                lock (gate)
                {
                    writer.Write(string.Create(CultureInfo.InvariantCulture, $"X {exitCode}\n"));
                    writer.Flush();
                }
            }

            if (result.EndsSession)
            {
                break;
            }
        }
    }
}
