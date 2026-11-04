namespace CyberInternauts.CmdStarter.Benchmarks.Services;

/// <summary>A service injected into a measured command when dependency injection is used.</summary>
public interface IBenchmarkService
{
    /// <summary>Handles a value and returns the exit code.</summary>
    /// <param name="value">The command argument.</param>
    /// <returns>Always 0.</returns>
    int Handle(string value);
}

/// <summary>Default <see cref="IBenchmarkService"/>, registered as a transient service.</summary>
public sealed class BenchmarkService : IBenchmarkService
{
    /// <inheritdoc/>
    public int Handle(string value) => 0;
}
