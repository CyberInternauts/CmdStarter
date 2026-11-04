using CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;
using Microsoft.Extensions.DependencyInjection;

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
    public async Task GetInstanceBeatsServiceProviderAndFactory()
    {
        // A command that declares GetInstance: neither the service provider nor the factory is used.
        var factoryCalls = 0;
        var providerCalls = 0;
        var services = new ServiceCollection();
        services.AddTransient(_ => { providerCalls++; return new SelfCreatedCommand("provider"); });
        services.AddTransient(_ => { providerCalls++; return new DisposableCommand(); });
        await using var provider = services.BuildServiceProvider();

        var selfCreated = CreateLifecycleStarter<SelfCreatedCommand>();
        selfCreated.ServiceProvider = provider;
        selfCreated.SetFactory(_ => { factoryCalls++; return new SelfCreatedCommand("factory"); });
        Assert.That(await selfCreated.Start(Array.Empty<string>()), Is.Zero);
        Assert.That(LifecycleLog.Entries, Is.EqualTo(new[] { "origin:get-instance" }));
        Assert.That(providerCalls, Is.Zero);
        Assert.That(factoryCalls, Is.Zero);

        // A command without GetInstance: the service provider is used before the factory.
        var withoutGetInstance = CreateLifecycleStarter<DisposableCommand>();
        withoutGetInstance.ServiceProvider = provider;
        withoutGetInstance.SetFactory(_ => { factoryCalls++; return new DisposableCommand(); });
        await withoutGetInstance.Start(Array.Empty<string>());
        Assert.That(providerCalls, Is.GreaterThan(0));
        Assert.That(factoryCalls, Is.Zero);
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
        var code = await starter.Start(["--option-inconnue"]);
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

    [Test]
    public async Task UnregisteredCommandUsesTheConstructorTheContainerCanSatisfy()
    {
        // The command type is not registered, so it is created from its constructors: the long one needs a service the container does not know.
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var starter = CreateLifecycleStarter<TwoConstructorsCommand>();
        starter.ServiceProvider = provider;

        Assert.That(await starter.Start(Array.Empty<string>()), Is.Zero);
        Assert.That(LifecycleLog.Entries, Is.EqualTo(new[] { "command:short" }));
    }

    [Test]
    public async Task UnregisteredMethodClassUsesTheConstructorTheContainerCanSatisfy()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Clear();
        starter.Classes = starter.Classes.Clear().Add(typeof(TwoConstructorsMethods).FullName!);
        starter.ServiceProvider = provider;

        Assert.That(await starter.Start("run"), Is.Zero);
        Assert.That(LifecycleLog.Entries, Is.EqualTo(new[] { "methods:short" }));
    }

    [Test]
    public async Task SynchronousInstantiationWithAServiceProviderReleasesTheDescriptionInstance()
    {
        var starter = CreateLifecycleStarter<DisposableCommand>();
        var services = new ServiceCollection();
        services.AddTransient<DisposableCommand>();
        await using var provider = services.BuildServiceProvider();
        starter.ServiceProvider = provider;

        // The tree is built without any async release: the description instance is disposed synchronously.
        starter.InstantiateCommands();

        Assert.That(LifecycleLog.Entries, Is.EqualTo(new[] { "dispose:1" }));
        Assert.That(await starter.Start(Array.Empty<string>()), Is.EqualTo(2));
    }

    private static Starter CreateLifecycleStarter<CommandType>() where CommandType : IStarterCommand
    {
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Clear();
        starter.Classes = starter.Classes.Clear().Add(typeof(CommandType).FullName!);
        return starter;
    }
}
