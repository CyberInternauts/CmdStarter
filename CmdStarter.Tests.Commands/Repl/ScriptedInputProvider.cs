namespace CyberInternauts.CmdStarter.Tests.Commands.Repl;

public sealed class ScriptedInputProvider(params string?[] lines) : IReplInputProvider
{
    private readonly Queue<string?> remainingLines = new(lines);

    public ValueTask<string?> GetInputAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(remainingLines.TryDequeue(out var line) ? line : null);
}
