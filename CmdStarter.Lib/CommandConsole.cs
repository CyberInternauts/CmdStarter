namespace CyberInternauts.CmdStarter;

/// <summary>
/// The input, output and error streams of one command execution. A handler parameter of this type receives the console
/// of its execution, so the output can go elsewhere than the process console (a remote session, a pipe, a test).
/// </summary>
public sealed class CommandConsole
{
    /// <summary>Creates a console from three streams.</summary>
    /// <param name="input">The standard input of the execution.</param>
    /// <param name="output">The standard output of the execution.</param>
    /// <param name="error">The standard error of the execution.</param>
    /// <exception cref="ArgumentNullException">A stream is null.</exception>
    public CommandConsole(TextReader input, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        In = input;
        Out = output;
        Error = error;
    }

    /// <summary>Gets the standard input of the execution.</summary>
    public TextReader In { get; }

    /// <summary>Gets the standard output of the execution.</summary>
    public TextWriter Out { get; }

    /// <summary>Gets the standard error of the execution.</summary>
    public TextWriter Error { get; }

    /// <summary>
    /// Gets a console over the streams of the process, read when this property is read:
    /// a <see cref="System.Console.SetOut(TextWriter)"/> done earlier is respected.
    /// </summary>
    public static CommandConsole Standard => new(global::System.Console.In, global::System.Console.Out, global::System.Console.Error);
}
