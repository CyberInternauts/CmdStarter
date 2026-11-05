namespace CyberInternauts.CmdStarter.Benchmarks.Commands.Classes;

/// <summary>
/// Base of the measured class commands: two options (<see cref="Count"/> and <see cref="Label"/>)
/// and one argument (<c>value</c>); the handler writes nothing and returns 0.
/// </summary>
public abstract class BenchmarkCommand : StarterCommand
{
    /// <summary>Gets or sets the first option.</summary>
    public int Count { get; set; }

    /// <summary>Gets or sets the second option.</summary>
    public string Label { get; set; } = string.Empty;

    /// <inheritdoc/>
    public override Delegate HandlingMethod => Execute;

    /// <summary>Handles the command and returns the exit code.</summary>
    /// <param name="value">The command argument.</param>
    /// <returns>The exit code.</returns>
    protected virtual int Run(string value) => 0;

    private int Execute(string value) => Run(value);
}
