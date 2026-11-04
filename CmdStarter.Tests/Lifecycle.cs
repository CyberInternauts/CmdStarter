using CyberInternauts.CmdStarter;
using CyberInternauts.CmdStarter.Interfaces;
using CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("CmdStarter")]
[Category("Lifecycle")]
public class Lifecycle
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [SetUp]
    public void Setup() => LifecycleLog.Reset();

    [Test]
    public async Task UsesFactoryOfItsOwnStarterOnly()
    {
        var calls = new List<Type>();
        var withFactory = CreateLifecycleStarter<DisposableCommand>();
        withFactory.SetFactory(type => { calls.Add(type); return new DisposableCommand(); });
        var withoutFactory = CreateLifecycleStarter<DisposableCommand>();
        await withoutFactory.Start(Array.Empty<string>());
        Assert.That(calls, Is.Empty);
        await withFactory.Start(Array.Empty<string>());
        Assert.That(calls, Is.Not.Empty);
        Assert.That(calls, Is.All.EqualTo(typeof(DisposableCommand)));
    }

    [Test]
    public async Task SetDefaultFactoryRestoresDefaultCreation()
    {
        var calls = new List<Type>();
        var starter = CreateLifecycleStarter<DisposableCommand>();
        starter.SetFactory(type => { calls.Add(type); return new DisposableCommand(); });
        starter.SetDefaultFactory();
        await starter.Start(Array.Empty<string>());
        Assert.That(calls, Is.Empty);
    }

    [Test]
    public void ThrowsWhenFactoryReturnsNull()
    {
        var starter = CreateLifecycleStarter<DisposableCommand>();
        starter.SetFactory(_ => null!);
        Assert.ThrowsAsync<InvalidOperationException>(() => starter.Start(Array.Empty<string>()));
    }

    [Test]
    public async Task RaisesCommandHandledAfterExecution()
    {
        var starter = CreateLifecycleStarter<DisposableCommand>();
        var received = new List<CommandHandledEventArgs>();
        starter.CommandHandled += (_, e) => received.Add(e);
        var code = await starter.Start(Array.Empty<string>());
        Assert.That(received, Has.Count.EqualTo(1));
        Assert.That(received[0].ExitCode, Is.EqualTo(code));
        Assert.That(received[0].Command, Is.SameAs(starter.FindCommand<DisposableCommand>()));
        Assert.That(received[0].Instance, Is.TypeOf<DisposableCommand>());
    }

    [Test]
    public async Task DoesNotRaiseCommandHandledOnParseError()
    {
        var starter = CreateLifecycleStarter<DisposableCommand>();
        var received = new List<CommandHandledEventArgs>();
        starter.CommandHandled += (_, e) => received.Add(e);
        var code = await starter.Start(new[] { "--option-inconnue" });
        Assert.That(code, Is.Not.EqualTo(0));
        Assert.That(received, Is.Empty);
    }

    [Test]
    public async Task CreatesNewInstanceForEachExecution()
    {
        var starter = CreateLifecycleStarter<DisposableCommand>();
        starter.InstantiateCommands();
        LifecycleLog.Reset();
        Assert.That(await starter.Start(Array.Empty<string>()), Is.EqualTo(1));
        Assert.That(await starter.Start(Array.Empty<string>()), Is.EqualTo(2));
        Assert.That(LifecycleLog.Entries, Is.EqualTo(new[] { "execute:1", "dispose:1", "execute:2", "dispose:2" }));
    }

    [Test]
    public void ReleasesDescriptionInstanceOnceTreeIsBuilt()
    {
        var starter = CreateLifecycleStarter<DisposableCommand>();
        starter.InstantiateCommands();
        Assert.That(LifecycleLog.Entries, Is.EqualTo(new[] { "dispose:1" }));
        Assert.That(starter.FindCommandInstance<DisposableCommand>()!.Id, Is.EqualTo(1));
    }

    [Test]
    public async Task DisposesAsyncDisposableCommand()
    {
        var starter = CreateLifecycleStarter<AsyncDisposableCommand>();
        starter.InstantiateCommands();
        Assert.That(LifecycleLog.Entries, Is.EqualTo(new[] { "dispose-async:1" }));
        await starter.Start(Array.Empty<string>());
        Assert.That(LifecycleLog.Entries, Is.EqualTo(new[] { "dispose-async:1", "execute:2", "dispose-async:2" }));
    }

    [Test]
    public async Task PrefersDisposeAsyncForExecutionInstances()
    {
        var starter = CreateLifecycleStarter<BothDisposableCommand>();
        starter.InstantiateCommands();
        Assert.That(LifecycleLog.Entries, Is.EqualTo(new[] { "dispose:1" }));
        await starter.Start(Array.Empty<string>());
        Assert.That(LifecycleLog.Entries, Is.EqualTo(new[] { "dispose:1", "execute:2", "dispose-async:2" }));
    }

    [Test]
    public async Task DisposesCommandWhenHandlerThrows()
    {
        var starter = CreateLifecycleStarter<ThrowingCommand>();
        var received = new List<CommandHandledEventArgs>();
        starter.CommandHandled += (_, e) => received.Add(e);
        starter.InstantiateCommands();
        LifecycleLog.Reset();
        var code = await starter.Start(Array.Empty<string>());
        Assert.That(code, Is.Not.EqualTo(0));
        Assert.That(LifecycleLog.Entries, Is.EqualTo(new[] { "execute:1", "dispose:1" }));
        Assert.That(received, Is.Empty);
    }

    [Test]
    public async Task DoesNotDisposeFactoryInstances()
    {
        var starter = CreateLifecycleStarter<DisposableCommand>();
        starter.SetFactory(_ => new DisposableCommand());
        starter.InstantiateCommands();
        LifecycleLog.Reset();
        await starter.Start(Array.Empty<string>());
        Assert.That(LifecycleLog.Entries, Is.EqualTo(new[] { "execute:1" }));
    }

    [Test]
    public async Task CallsFactoryOnceForTheTreeAndOncePerExecution()
    {
        var calls = 0;
        var starter = CreateLifecycleStarter<DisposableCommand>();
        starter.SetFactory(_ => { calls++; return new DisposableCommand(); });
        starter.InstantiateCommands();
        Assert.That(calls, Is.EqualTo(1));
        await starter.Start(Array.Empty<string>());
        await starter.Start(Array.Empty<string>());
        Assert.That(calls, Is.EqualTo(3));
    }

    [Test]
    public async Task CommandHandledProvidesTheExecutionInstance()
    {
        var starter = CreateLifecycleStarter<DisposableCommand>();
        DisposableCommand? executed = null;
        starter.CommandHandled += (_, e) => executed = (DisposableCommand)e.Instance!;
        var code = await starter.Start(Array.Empty<string>());
        Assert.That(executed, Is.Not.SameAs(starter.FindCommandInstance<DisposableCommand>()));
        Assert.That(executed!.Id, Is.EqualTo(code));
    }

    private static Starter CreateLifecycleStarter<CommandType>() where CommandType : IStarterCommand
    {
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Clear();
        starter.Classes = starter.Classes.Clear().Add(typeof(CommandType).FullName!);
        return starter;
    }
}
