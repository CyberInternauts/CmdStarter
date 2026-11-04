using CyberInternauts.CmdStarter.Tests.Commands.Filters;
using CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;
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
        ScopedDisposable.Disposals = 0;
        ScopedResolvingFilter.Resolved.Clear();
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
    public async Task FilterContextGivesTheCommandBeingExecuted()
    {
        CommandContext? captured = null;
        var starter = CreateStarter().AddFilter(new ContextFilter(context => captured = context));

        await starter.Start("probe");
        var executed = captured!.Command;
        await starter.Start("probe-method");

        Assert.That(executed, Is.SameAs(starter.FindCommand<Probe>()));
        Assert.That(executed.Name, Is.EqualTo("probe"));
        Assert.That(captured.Command.Name, Is.EqualTo("probe-method"));
        Assert.That(captured.Command, Is.SameAs(starter.RootCommand.Subcommands.Single(command => command.Name == "probe-method")));
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
                starter.Namespaces = [typeof(Probe).Namespace!];
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
                starter.Namespaces = [typeof(Probe).Namespace!];
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

    [Test]
    public async Task FilterRegisteredByTypeGetsItsServicesWhenTheCommandComesFromGetInstance()
    {
        var marker = new Marker();
        await using var provider = new ServiceCollection().AddSingleton<IMarker>(marker).BuildServiceProvider();
        var starter = CreateGetInstanceStarter(provider).AddFilter<MarkingFilter>();
        var (console, error) = CreateConsole();

        var exitCode = await starter.Start([], console);

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(0));
            Assert.That(error.ToString(), Is.Empty);
            Assert.That(marker.Marks, Is.EqualTo(new[] { "filter: marked" }));
        });
    }

    [Test]
    public async Task CommandContextHasServicesWhenTheCommandComesFromGetInstance()
    {
        ServicesProbeFilter.ServicesWereNull = null;
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var starter = CreateGetInstanceStarter(provider).AddFilter<ServicesProbeFilter>();

        Assert.That(await starter.Start([]), Is.EqualTo(0));
        Assert.That(ServicesProbeFilter.ServicesWereNull, Is.False);
    }

    [Test]
    public async Task ExecutionScopeIsReleasedAfterACommandFromGetInstance()
    {
        await using var provider = new ServiceCollection().AddScoped<ScopedDisposable>().BuildServiceProvider();
        var scopes = new CountingScopes(provider);
        var starter = CreateGetInstanceStarter(scopes).AddFilter<ScopedResolvingFilter>();

        // Building the tree activates the description instance of the command through the same path.
        starter.InstantiateCommands();
        var openedByTheTree = scopes.Opened;
        Assert.That(openedByTheTree, Is.GreaterThan(0));
        Assert.That(scopes.Closed, Is.EqualTo(scopes.Opened));

        Assert.That(await starter.Start([]), Is.EqualTo(0));

        Assert.That(scopes.Opened, Is.EqualTo(openedByTheTree + 1));
        Assert.That(scopes.Closed, Is.EqualTo(scopes.Opened));
        Assert.That(ScopedResolvingFilter.Resolved, Has.Count.EqualTo(1));
        Assert.That(ScopedDisposable.Disposals, Is.EqualTo(1));
    }

    [Test]
    public async Task GetInstanceStillCreatesTheCommand()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var starter = CreateGetInstanceStarter(provider);
        MadeCommand? executed = null;
        starter.CommandHandled += (_, eventArgs) => executed = (MadeCommand?)eventArgs.Instance;

        Assert.That(await starter.Start([]), Is.EqualTo(0));

        Assert.That(executed, Is.Not.Null);
        Assert.That(executed!.Origin, Is.EqualTo("GetInstance"));
        Assert.That(executed.Disposed, Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetInstanceFailureReleasesTheExecutionScope(bool providerHasNoScopeFactory)
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var scopes = new CountingScopes(provider, providerHasNoScopeFactory);
        var starter = new Starter
        {
            Classes = [typeof(FailingGetInstanceCommand).FullName!],
            ServiceProvider = scopes
        };

        Assert.ThrowsAsync<System.Reflection.TargetInvocationException>(async () => await starter.Start([]));

        Assert.That(scopes.Closed, Is.EqualTo(scopes.Opened));
        Assert.That(scopes.Opened, providerHasNoScopeFactory ? Is.EqualTo(0) : Is.GreaterThan(0));
    }

    [Test]
    public async Task FactoryPathIsUnchanged()
    {
        var starter = new Starter
        {
            Classes = [typeof(Probe).FullName!]
        };
        starter.SetFactory(type => (IStarterCommand)Activator.CreateInstance(type)!);
        starter.AddFilter<ServicesProbeFilter>();
        ServicesProbeFilter.ServicesWereNull = null;

        Assert.That(await starter.Start([]), Is.EqualTo(0));

        // Without a service provider there is no execution scope: the filter is created with its parameterless constructor.
        Assert.That(ServicesProbeFilter.ServicesWereNull, Is.True);
        Assert.That(FilterLog.Current.Entries, Is.EqualTo(new[] { "handler" }));
    }
    private static Starter CreateGetInstanceStarter(IServiceProvider? provider) => new()
    {
        Classes = [typeof(MadeCommand).FullName!],
        ServiceProvider = provider
    };

    private static (CommandConsole Console, StringWriter Error) CreateConsole()
    {
        var error = new StringWriter();
        return (new CommandConsole(new StringReader(string.Empty), new StringWriter(), error), error);
    }
    private static Starter CreateStarter() => new()
    {
        Namespaces = [typeof(Probe).Namespace!],
        IsRootingLonelyCommand = false,
        StateProvider = new TestStateProvider { CurrentState = "open" }
    };

    private sealed class CountingScopes(IServiceProvider inner, bool hidesScopeFactory = false) : IServiceProvider, IServiceScopeFactory
    {
        public int Opened { get; private set; }

        public int Closed { get; private set; }

        public object? GetService(Type serviceType) =>
            serviceType == typeof(IServiceScopeFactory) ? (hidesScopeFactory ? null : this) : inner.GetService(serviceType);

        public IServiceScope CreateScope()
        {
            Opened++;
            return new CountedScope(inner.CreateScope(), this);
        }

        private sealed class CountedScope(IServiceScope scope, CountingScopes owner) : IServiceScope, IAsyncDisposable
        {
            public IServiceProvider ServiceProvider => scope.ServiceProvider;

            public void Dispose()
            {
                owner.Closed++;
                scope.Dispose();
            }

            public ValueTask DisposeAsync()
            {
                owner.Closed++;
                if (scope is IAsyncDisposable asyncDisposable) return asyncDisposable.DisposeAsync();
                scope.Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
    private sealed class ContextFilter(Action<CommandContext> capture) : ICommandFilter
    {
        public Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next)
        {
            capture(context);
            return next();
        }
    }
}

