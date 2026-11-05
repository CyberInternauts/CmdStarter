using CyberInternauts.CmdStarter.Tests.Commands.ExecutionPaths;

namespace CyberInternauts.CmdStarter.Tests;

/// <summary>
/// Execution paths of method commands that no process-level test shows: failures, missing option values and service providers without scopes.
/// Every test runs in memory, through Start and a CommandConsole.
/// </summary>
[Category("* All *")]
[Category("CmdStarter")]
[Category("ExecutionPaths")]
public class StarterExecutionPaths
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [SetUp]
    public void Setup()
    {
        ExecutionLog.Entries.Clear();
        PlainMethods.Last = null;
    }

    [Test]
    public async Task MethodCommandExceptionIsReportedOnOneLine()
    {
        var starter = CreateStarter(typeof(ThrowingMethods));
        using var error = new StringWriter();

        var code = await starter.Start("fail-method", ConsoleWith(error));

        Assert.That(code, Is.EqualTo(1));
        Assert.That(error.ToString().TrimEnd(), Is.EqualTo("first line second line"));
    }

    [Test]
    public async Task OptionParametersWithoutValueReceiveTheirTypeDefault()
    {
        var starter = CreateStarter(typeof(OptionMethods));
        using var error = new StringWriter();

        var code = await starter.Start("show-options", ConsoleWith(error));

        Assert.That(code, Is.Zero, error.ToString());
        Assert.That(ExecutionLog.Entries, Is.EqualTo(new[] { "count=0;name=<null>;label=<null>" }));
    }

    [Test]
    public async Task OptionParametersTakeTheirGivenValue()
    {
        var starter = CreateStarter(typeof(OptionMethods));
        using var error = new StringWriter();

        var code = await starter.Start("show-options --count 4 --name bob --label x", ConsoleWith(error));

        Assert.That(code, Is.Zero, error.ToString());
        Assert.That(ExecutionLog.Entries, Is.EqualTo(new[] { "count=4;name=bob;label=x" }));
    }

    [Test]
    public async Task ArrayArgumentWithoutValuesReachesTheHandler()
    {
        var starter = CreateStarter(typeof(ArrayMethods));
        using var error = new StringWriter();

        var code = await starter.Start("take-values", ConsoleWith(error));

        Assert.That(code, Is.Zero, error.ToString());
        Assert.That(ExecutionLog.Entries, Is.EqualTo(new[] { "values=[]" }));
    }

    [Test]
    public async Task ProviderWithoutScopesCreatesAndReleasesTheMethodClassInstance()
    {
        var starter = CreateStarter(typeof(PlainMethods));
        starter.ServiceProvider = new MinimalProvider();
        using var error = new StringWriter();

        var code = await starter.Start("plain-instance", ConsoleWith(error));

        Assert.That(code, Is.EqualTo(5), error.ToString());
        Assert.That(PlainMethods.Last!.IsDisposed, Is.True);
    }

    [Test]
    public async Task ProviderWithoutScopesKeepsTheInstanceItRegistered()
    {
        var registered = new PlainMethods();
        var starter = CreateStarter(typeof(PlainMethods));
        starter.ServiceProvider = new MinimalProvider(registered);
        using var error = new StringWriter();

        var code = await starter.Start("plain-instance", ConsoleWith(error));

        Assert.That(code, Is.EqualTo(5), error.ToString());
        Assert.That(PlainMethods.Last, Is.SameAs(registered));
        Assert.That(registered.IsDisposed, Is.False);
    }

    [Test]
    public async Task ProviderWithoutScopesRunsStaticMethods()
    {
        var starter = CreateStarter(typeof(PlainMethods));
        starter.ServiceProvider = new MinimalProvider();
        using var error = new StringWriter();

        Assert.That(await starter.Start("plain-static", ConsoleWith(error)), Is.EqualTo(6), error.ToString());
    }

    [Test]
    public void ProviderThatCannotTellServicesLeavesClassParametersAsArguments()
    {
        var starter = CreateStarter(typeof(PlainMethods));
        starter.ServiceProvider = new MinimalProvider();

        starter.InstantiateCommands();

        var command = starter.RootCommand.Subcommands.Single(subcommand => subcommand.Name == "plain-service");
        Assert.That(command.Arguments.Select(argument => argument.Name), Is.EqualTo(new[] { "service" }));
    }

    [Test]
    public async Task ServiceParameterFailsWhenTheProviderIsRemovedAfterTheTreeIsBuilt()
    {
        var starter = CreateStarter(typeof(PlainMethods));
        starter.ServiceProvider = new MinimalProvider(new PlainService());
        starter.InstantiateCommands();
        starter.ServiceProvider = null;
        using var error = new StringWriter();

        var code = await starter.Start("needs-service", ConsoleWith(error));

        Assert.That(code, Is.EqualTo(1));
        Assert.That(error.ToString(), Does.Contain("No service provider is available for parameter 'service' of method 'NeedsService'"));
    }

    [Test]
    public async Task ClassWithoutNameBecomesAGroupNamedAfterItsType()
    {
        var starter = CreateStarter(typeof(UnnamedGroupMethods));
        using var error = new StringWriter();

        var code = await starter.Start("unnamed-group-methods go", ConsoleWith(error));

        Assert.That(code, Is.EqualTo(7), error.ToString());
    }

    private static Starter CreateStarter(Type type)
    {
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Clear();
        starter.Classes = starter.Classes.Clear().Add(type.FullName!);
        return starter;
    }

    private static CommandConsole ConsoleWith(StringWriter error) => new(new StringReader(string.Empty), new StringWriter(), error);

    // A provider that only knows the instances it is given: no scope factory and no way to tell whether a type is a service.
    private sealed class MinimalProvider(params object[] services) : IServiceProvider
    {
        public object? GetService(Type serviceType) => services.FirstOrDefault(serviceType.IsInstanceOfType);
    }
}
