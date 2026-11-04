using CyberInternauts.CmdStarter.Interfaces;
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
    public async Task CancellationInsideFilterReturns130ForClassCommand()
    {
        using var cts = new CancellationTokenSource();
        var starter = CreateFilterStarter(cts);
        var handled = false;
        starter.CommandHandled += (_, _) => handled = true;

        Assert.That(await starter.Start(new[] { "probe" }, cts.Token), Is.EqualTo(130));
        Assert.That(handled, Is.False);
    }

    [Test]
    public async Task CancellationInsideFilterReturns130ForMethodCommand()
    {
        using var cts = new CancellationTokenSource();
        var starter = CreateFilterStarter(cts);
        var handled = false;
        starter.CommandHandled += (_, _) => handled = true;

        Assert.That(await starter.Start(new[] { "probe-method" }, cts.Token), Is.EqualTo(130));
        Assert.That(handled, Is.False);
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

    // A filter that cancels the invocation and then checks its token, before the handler runs.
    private static Starter CreateFilterStarter(CancellationTokenSource source)
    {
        var starter = new Starter
        {
            Namespaces = System.Collections.Immutable.ImmutableList.Create(typeof(Probe).Namespace!),
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
