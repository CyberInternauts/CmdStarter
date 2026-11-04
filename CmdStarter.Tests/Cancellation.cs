using CyberInternauts.CmdStarter.Tests.Commands.Cancellation;
using CyberInternauts.CmdStarter.Tests.Commands.Filters;
using CyberInternauts.CmdStarter.Tests.Commands.States;

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
            Namespaces = [typeof(CancellableCommand).Namespace!],
            Classes = [typeof(CancellableCommand).FullName!]
        };
        CancellableCommand.Reset();

        var run = starter.Start([], cts.Token);
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
            Namespaces = [typeof(CancellableCommand).Namespace!],
            Classes = [typeof(CancellableMethods).FullName!]
        };
        CancellableMethods.Reset();

        var run = starter.Start(["wait-for-cancel"], cts.Token);
        await CancellableMethods.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cts.Cancel();

        Assert.That(await run.WaitAsync(TimeSpan.FromSeconds(10)), Is.EqualTo(130));
    }

    [Test]
    public async Task CancellationInsideFilterReturns130ForClassCommand()
    {
        using var cts = new CancellationTokenSource();
        var starter = CreateFilterStarter(cts);
        var handled = false;
        starter.CommandHandled += (_, _) => handled = true;

        Assert.That(await starter.Start(["probe"], cts.Token), Is.EqualTo(130));
        Assert.That(handled, Is.False);
    }

    [Test]
    public async Task CancellationInsideFilterReturns130ForMethodCommand()
    {
        using var cts = new CancellationTokenSource();
        var starter = CreateFilterStarter(cts);
        var handled = false;
        starter.CommandHandled += (_, _) => handled = true;

        Assert.That(await starter.Start(["probe-method"], cts.Token), Is.EqualTo(130));
        Assert.That(handled, Is.False);
    }

    [Test]
    public void CancellationTokenParameterIsNotAnArgument()
    {
        var starter = new Starter
        {
            Namespaces = [typeof(CancellableCommand).Namespace!],
            Classes = [typeof(CancellableCommand).FullName!]
        };

        starter.InstantiateCommands();

        Assert.That(starter.RootCommand.Arguments, Is.Empty);
    }

    // A filter that cancels the invocation and then checks its token, before the handler runs.
    private static Starter CreateFilterStarter(CancellationTokenSource source)
    {
        var starter = new Starter
        {
            Namespaces = [typeof(Probe).Namespace!],
            IsRootingLonelyCommand = false,
            StateProvider = new TestStateProvider { CurrentState = "open" }
        };
        return starter.AddFilter(new CancellingFilter(source));
    }

    private sealed class CancellingFilter(CancellationTokenSource source) : ICommandFilter
    {
        public Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next)
        {
            source.Cancel();
            context.CancellationToken.ThrowIfCancellationRequested();
            return next();
        }
    }
}
