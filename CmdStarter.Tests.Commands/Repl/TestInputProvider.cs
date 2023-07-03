namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Repl
{
    public sealed class TestInputProvider : IReplInputProvider
    {
        public readonly Queue<string> CommandQueue = new Queue<string>();

        public string GetInput()
        {
            var command = CommandQueue.Dequeue();
            CommandQueue.Enqueue(command);

            return command;
        }
    }
}
