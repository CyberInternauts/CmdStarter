using CyberInternauts.CmdStarter.Tests.Commands.Cancellation;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("Cancellation")]
public class Cancellation
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [Test]
    public async Task ClassCommandReturns130WhenCancelled()
    {
        using var cts = new CancellationTokenSource();
        var starter = new Starter
        {
            Namespaces = System.Collections.Immutable.ImmutableList.Create(typeof(CancellableCommand).Namespace!),
            Classes = System.Collections.Immutable.ImmutableList.Create(typeof(CancellableCommand).FullName!)
        };
        CancellableCommand.Reset();

        var run = starter.Start(Array.Empty<string>(), cts.Token);
        await CancellableCommand.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cts.Cancel();

        Assert.That(await run.WaitAsync(TimeSpan.FromSeconds(10)), Is.EqualTo(130));
    }

    [Test]
    public async Task MethodCommandReturns130WhenCancelled()
    {
        using var cts = new CancellationTokenSource();
        var starter = new Starter
        {
            Namespaces = System.Collections.Immutable.ImmutableList.Create(typeof(CancellableCommand).Namespace!),
            Classes = System.Collections.Immutable.ImmutableList.Create(typeof(CancellableMethods).FullName!)
        };
        CancellableMethods.Reset();

        var run = starter.Start(new[] { "wait-for-cancel" }, cts.Token);
        await CancellableMethods.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cts.Cancel();

        Assert.That(await run.WaitAsync(TimeSpan.FromSeconds(10)), Is.EqualTo(130));
    }

    [Test]
    public void CancellationTokenParameterIsNotAnArgument()
    {
        var starter = new Starter
        {
            Namespaces = System.Collections.Immutable.ImmutableList.Create(typeof(CancellableCommand).Namespace!),
            Classes = System.Collections.Immutable.ImmutableList.Create(typeof(CancellableCommand).FullName!)
        };

        starter.InstantiateCommands();

        Assert.That(starter.RootCommand.Arguments, Is.Empty);
    }
}
