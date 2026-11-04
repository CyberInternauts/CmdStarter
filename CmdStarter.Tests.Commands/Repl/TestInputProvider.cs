namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Repl;

public sealed class TestInputProvider : IReplInputProvider
{
    public readonly Queue<string> CommandQueue = new Queue<string>();

    public ValueTask<string?> GetInputAsync(CancellationToken cancellationToken)
    {
        var command = CommandQueue.Dequeue();
        CommandQueue.Enqueue(command);

        return ValueTask.FromResult<string?>(command);
    }
}
