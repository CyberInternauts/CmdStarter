using System.Reflection;
using CyberInternauts.CmdStarter.Interfaces;
using CyberInternauts.CmdStarter.Tests.Commands.Filters;
using CyberInternauts.CmdStarter.Tests.Commands.States;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("Filters")]
public sealed class Filters
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [SetUp]
    public void Setup()
    {
        FilterLog.Current = new FilterLog();
        DisposableFilter.Disposals = 0;
        ScopedFilter.ResolvedServices.Clear();
    }

    [Test]
    public async Task FiltersRunInRegistrationOrder()
    {
        var log = FilterLog.Current;
        var starter = CreateStarter()
            .AddFilter(new AroundFilter(log, "A"))
            .AddFilter(new AroundFilter(log, "B"));

        Assert.That(await starter.Start("probe"), Is.EqualTo(0));
        Assert.That(log.Entries, Is.EqualTo(new[] { "A-before", "B-before", "handler", "B-after", "A-after" }));
    }

    [Test]
    public async Task FilterCanShortCircuit()
    {
        var handled = false;
        var starter = CreateStarter().AddFilter(new ShortCircuitFilter(7));
        starter.CommandHandled += (_, _) => handled = true;

        Assert.That(await starter.Start("probe"), Is.EqualTo(7));
        Assert.That(FilterLog.Current.Entries, Is.Empty);
        Assert.That(handled, Is.False);
    }

    [Test]
    public async Task FilterCanChangeExitCode()
    {
        var handledCode = -1;
        var starter = CreateStarter().AddFilter<IncrementFilter>();
        starter.CommandHandled += (_, eventArgs) => handledCode = eventArgs.ExitCode;

        Assert.That(await starter.Start("probe --value 5"), Is.EqualTo(6));
        Assert.That(handledCode, Is.EqualTo(5));
    }

    [Test]
    public async Task FilterSeesBoundContext()
    {
        CommandContext? captured = null;
        var starter = CreateStarter().AddFilter(new ContextFilter(context => captured = context));

        await starter.Start("probe --value 5");

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.CommandPath, Is.EqualTo("probe"));
        Assert.That(captured.Member, Is.EqualTo(typeof(Probe)));
        Assert.That(captured.Instance, Is.TypeOf<Probe>());
        Assert.That(((Probe)captured.Instance!).Value, Is.EqualTo(5));
        Assert.That(captured.ParseResult, Is.Not.Null);
    }

    [Test]
    public async Task FiltersApplyToMethodCommands()
    {
        CommandContext? captured = null;
        var starter = CreateStarter().AddFilter(new ContextFilter(context => captured = context));

        Assert.That(await starter.Start("probe-method"), Is.EqualTo(21));
        Assert.That(captured!.CommandPath, Is.EqualTo("probe-method"));
        Assert.That(captured.Member, Is.EqualTo(typeof(ProbeMethods).GetMethod(nameof(ProbeMethods.Run))));
    }

    [Test]
    public async Task FilterExceptionIsReportedLikeHandlerException()
    {
        var starter = CreateStarter().AddFilter<ThrowingFilter>();

        Assert.That(await starter.Start("probe"), Is.EqualTo(1));
    }

    [Test]
    public async Task UnavailableCommandSkipsFilters()
    {
        var handled = false;
        var starter = CreateStarter().AddFilter(new ShortCircuitFilter(7));
        starter.StateProvider = new TestStateProvider { CurrentState = "closed" };
        starter.CommandHandled += (_, _) => handled = true;

        Assert.That(await starter.Start("locked"), Is.EqualTo(1));
        Assert.That(handled, Is.False);
    }

    [Test]
    public async Task CreatedFilterIsDisposedAfterEachExecution()
    {
        var starter = CreateStarter().AddFilter<DisposableFilter>();

        await starter.Start("probe");
        await starter.Start("probe");

        Assert.That(DisposableFilter.Disposals, Is.EqualTo(2));
    }

    [Test]
    public async Task CreatedAsyncDisposableFilterIsDisposedAsynchronously()
    {
        AsyncDisposableFilter.AsyncDisposals = 0;
        var starter = CreateStarter().AddFilter<AsyncDisposableFilter>();

        await starter.Start("probe");
        await starter.Start("probe");

        Assert.That(AsyncDisposableFilter.AsyncDisposals, Is.EqualTo(2));
    }

    [Test]
    public async Task FilterTypeUnknownToTheContainerIsCreatedWithItsConstructor()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var starter = CreateStarter().AddFilter<IncrementFilter>();
        starter.ServiceProvider = provider;

        Assert.That(await starter.Start("probe --value 5"), Is.EqualTo(6));
    }

    [Test]
    public async Task FilterTypeIsResolvedInExecutionScope()
    {
        var builder = new CmdStarterBuilder()
            .AddFilter<ScopedFilter>()
            .ConfigureStarter(starter =>
            {
                starter.Namespaces = System.Collections.Immutable.ImmutableList.Create(typeof(Probe).Namespace!);
                starter.IsRootingLonelyCommand = false;
            });
        builder.Services.AddScoped<FilterScopedService>();
        builder.Services.AddSingleton<ICommandStateProvider>(new TestStateProvider { CurrentState = "open" });
        await using var app = builder.Build();

        await app.RunAsync("probe");
        await app.RunAsync("probe");

        Assert.That(ScopedFilter.ResolvedServices, Has.Count.EqualTo(2));
        Assert.That(ScopedFilter.ResolvedServices[0], Is.Not.SameAs(ScopedFilter.ResolvedServices[1]));
    }

    [Test]
    public async Task OptionsAddFilterRunsFilterAndRegistersItAsTransient()
    {
        var services = new ServiceCollection();
        services.AddCmdStarter(options => options
            .ConfigureStarter(starter =>
            {
                starter.Namespaces = System.Collections.Immutable.ImmutableList.Create(typeof(Probe).Namespace!);
                starter.IsRootingLonelyCommand = false;
            })
            .AddFilter<ScopedFilter>());
        services.AddScoped<FilterScopedService>();
        services.AddSingleton<ICommandStateProvider>(new TestStateProvider { CurrentState = "open" });
        await using var provider = services.BuildServiceProvider();

        Assert.That(await provider.GetRequiredService<Starter>().Start("probe"), Is.EqualTo(0));
        Assert.That(ScopedFilter.ResolvedServices, Has.Count.EqualTo(1));

        // The filter type is registered as a transient service: each resolution gives a new instance.
        using var scope = provider.CreateScope();
        var first = scope.ServiceProvider.GetRequiredService<ScopedFilter>();
        var second = scope.ServiceProvider.GetRequiredService<ScopedFilter>();
        Assert.That(first, Is.Not.SameAs(second));
    }

    private static Starter CreateStarter() => new()
    {
        Namespaces = System.Collections.Immutable.ImmutableList.Create(typeof(Probe).Namespace!),
        IsRootingLonelyCommand = false,
        StateProvider = new TestStateProvider { CurrentState = "open" }
    };

    private sealed class ContextFilter(Action<CommandContext> capture) : ICommandFilter
    {
        public Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next)
        {
            capture(context);
            return next();
        }
    }
}
