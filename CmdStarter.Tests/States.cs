using CyberInternauts.CmdStarter.Tests.Commands.States;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("States")]
public class States
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [Test]
    public async Task HidesCommandsUnavailableInCurrentState()
    {
        var provider = new TestStateProvider { CurrentState = TestState.Anonymous };
        var starter = CreateStarter(provider);

        await starter.Start("always");

        Assert.That(starter.FindCommand<Enter>()!.Hidden, Is.False);
        Assert.That(starter.FindCommand<Profile>()!.Hidden, Is.True);
        Assert.That(starter.FindCommand<Always>()!.Hidden, Is.False);
        Assert.That(starter.FindCommand<AccountSettings>()!.Hidden, Is.True);
        Assert.That(starter.RootCommand.Subcommands.Single(command => command.Name == "upgrade").Hidden, Is.True);
    }

    [Test]
    public async Task HiddenCommandStaysHiddenWhenItsStateIsCurrent()
    {
        var provider = new TestStateProvider { CurrentState = TestState.Anonymous };
        var starter = CreateStarter(provider);

        Assert.That(await starter.Start("secret"), Is.EqualTo(80));
        Assert.That(starter.FindCommand<Secret>()!.Hidden, Is.True);
    }

    [Test]
    public async Task UnavailableCommandIsRefused()
    {
        var starter = CreateStarter(new TestStateProvider { CurrentState = TestState.Anonymous });

        Assert.That(await starter.Start("profile"), Is.EqualTo(1));
    }

    [Test]
    public async Task AvailabilityFollowsStateChanges()
    {
        var provider = new TestStateProvider { CurrentState = TestState.Anonymous };
        var starter = CreateStarter(provider);

        Assert.That(await starter.Start("enter"), Is.EqualTo(10));
        provider.CurrentState = TestState.Member;
        Assert.That(await starter.Start("enter"), Is.EqualTo(1));
        Assert.That(await starter.Start("profile"), Is.EqualTo(20));
        Assert.That(await starter.Start("account account-settings"), Is.EqualTo(50));
        Assert.That(starter.FindCommand<Enter>()!.Hidden, Is.True);
    }

    [Test]
    public async Task SeveralStatesInOneAttribute()
    {
        var provider = new TestStateProvider { CurrentState = TestState.Anonymous };
        var starter = CreateStarter(provider);

        Assert.That(await starter.Start("either"), Is.EqualTo(70));
        provider.CurrentState = TestState.Member;
        Assert.That(await starter.Start("either"), Is.EqualTo(70));
        provider.CurrentState = "another state";
        Assert.That(await starter.Start("either"), Is.EqualTo(1));
    }

    [Test]
    public async Task MethodCommandFollowsState()
    {
        var provider = new TestStateProvider { CurrentState = TestState.Anonymous };
        var starter = CreateStarter(provider);

        Assert.That(await starter.Start("upgrade"), Is.EqualTo(1));
        provider.CurrentState = TestState.Member;
        Assert.That(await starter.Start("upgrade"), Is.EqualTo(40));
    }

    [Test]
    public async Task PathGroupFollowsState()
    {
        var provider = new TestStateProvider { CurrentState = TestState.Anonymous };
        var starter = CreateStarter(provider);

        Assert.That(await starter.Start("vault secrets list"), Is.EqualTo(1));
        var vault = starter.RootCommand.Subcommands.Single(command => command.Name == "vault");
        Assert.That(vault.Hidden, Is.False);
        Assert.That(vault.Subcommands.Single(command => command.Name == "secrets").Hidden, Is.True);

        provider.CurrentState = TestState.Member;
        Assert.That(await starter.Start("vault secrets list"), Is.EqualTo(60));
    }

    [Test]
    public async Task StateProviderResolvedFromServices()
    {
        var provider = new TestStateProvider { CurrentState = TestState.Member };
        var builder = new CmdStarterBuilder()
            .AddCommandsFromAssembly(typeof(Enter).Assembly)
            .ConfigureStarter(starter => starter.Namespaces = System.Collections.Immutable.ImmutableList.Create(typeof(Enter).Namespace!));
        builder.Services.AddSingleton<ICommandStateProvider>(provider);
        await using var app = builder.Build();

        Assert.That(await app.RunAsync("profile"), Is.EqualTo(20));
    }

    [Test]
    public async Task LonelyRootedCommandStaysHiddenAndIsRefused()
    {
        var provider = new TestStateProvider { CurrentState = TestState.Member };
        var starter = CreateStarter(provider);
        starter.Classes = System.Collections.Immutable.ImmutableList.Create(typeof(Enter).FullName!);

        Assert.That(await starter.Start(Array.Empty<string>()), Is.EqualTo(1));
        Assert.That(starter.FindCommand<Enter>()!.Hidden, Is.True);
        provider.CurrentState = TestState.Anonymous;
        Assert.That(await starter.Start(Array.Empty<string>()), Is.EqualTo(10));
        Assert.That(starter.FindCommand<Enter>()!.Hidden, Is.True);
    }

    [Test]
    public async Task CommandAddedByApplicationIsLeftAsIs()
    {
        var starter = CreateStarter(new TestStateProvider { CurrentState = TestState.Anonymous });
        starter.InstantiateCommands();
        var manual = new System.CommandLine.Command("manual") { Hidden = true };
        starter.RootCommand.Subcommands.Add(manual);

        Assert.That(await starter.Start("always"), Is.EqualTo(30));
        Assert.That(manual.Hidden, Is.True);
    }

    [Test]
    public void MissingStateProviderThrows()
    {
        var starter = CreateStarter(null);
        starter.StateProvider = null;
        starter.ServiceProvider = null;

        Assert.Throws<InvalidAttributeException>(() => starter.InstantiateCommands());
    }

    private static Starter CreateStarter(TestStateProvider? provider)
    {
        var starter = new Starter
        {
            Namespaces = System.Collections.Immutable.ImmutableList.Create(typeof(Enter).Namespace!),
            StateProvider = provider
        };
        return starter;
    }
}
