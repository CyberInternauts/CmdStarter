using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Lifecycle;

namespace com.cyberinternauts.csharp.CmdStarter.Tests;

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

    private static Starter CreateLifecycleStarter<CommandType>() where CommandType : IStarterCommand
    {
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Clear();
        starter.Classes = starter.Classes.Clear().Add(typeof(CommandType).FullName!);
        return starter;
    }
}
