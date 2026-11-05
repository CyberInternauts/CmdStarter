namespace CyberInternauts.CmdStarter.Tests.Commands.Repl;

public sealed class BlockingInputProvider : IReplInputProvider
{
    public async ValueTask<string?> GetInputAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return null;
    }
}
