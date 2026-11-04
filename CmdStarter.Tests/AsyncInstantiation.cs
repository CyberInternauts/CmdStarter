using CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("Lifecycle")]
public class AsyncInstantiation
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [SetUp]
    public void Setup()
    {
        AsyncDisposableDescription.AsyncDisposals = 0;
        GatedAsyncDisposableDescription.AsyncDisposals = 0;
        GatedAsyncDisposableDescription.Gate = GatedAsyncDisposableDescription.NewGate();
    }

    [TearDown]
    public void TearDown() => GatedAsyncDisposableDescription.Gate.TrySetResult(); // Never leave a release waiting.

    [Test]
    public async Task InstantiateCommandsAsyncDoesNotBlockOnRelease()
    {
        var starter = CreateStarter(typeof(GatedAsyncDisposableDescription));

        // The release of the description instance waits for the gate: the call is still running, and the caller is not blocked.
        var task = starter.InstantiateCommandsAsync();
        Assert.That(task.IsCompleted, Is.False);
        Assert.That(GatedAsyncDisposableDescription.AsyncDisposals, Is.Zero);

        GatedAsyncDisposableDescription.Gate.SetResult();
        await task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(GatedAsyncDisposableDescription.AsyncDisposals, Is.EqualTo(1));
    }

    [Test]
    public async Task InstantiateCommandsAsyncReleasesDescriptionAsynchronously()
    {
        var starter = CreateStarter();

        await starter.InstantiateCommandsAsync();
        Assert.That(AsyncDisposableDescription.AsyncDisposals, Is.EqualTo(1));

        // The commands are already built: a second call does nothing.
        await starter.InstantiateCommandsAsync();
        Assert.That(AsyncDisposableDescription.AsyncDisposals, Is.EqualTo(1));
    }

    [Test]
    public async Task StartReleasesDescriptionAndExecutionInstances()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start(Array.Empty<string>()), Is.Zero);

        // The description instance (when the tree is built), then the instance of the execution.
        Assert.That(AsyncDisposableDescription.AsyncDisposals, Is.EqualTo(2));
    }

    private static Starter CreateStarter(Type? commandType = null)
    {
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Clear();
        starter.Classes = starter.Classes.Clear().Add((commandType ?? typeof(AsyncDisposableDescription)).FullName!);
        return starter;
    }
}
