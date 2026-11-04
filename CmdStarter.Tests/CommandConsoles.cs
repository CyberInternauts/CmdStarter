using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Interfaces;
using CyberInternauts.CmdStarter.Repl;
using CyberInternauts.CmdStarter.Tests.Commands.Consoles;
using CyberInternauts.CmdStarter.Tests.Commands.Repl;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("Console")]
public class CommandConsoles
{
    private StringWriter output = new();
    private StringWriter error = new();

    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [SetUp]
    public void Setup()
    {
        output = new StringWriter();
        error = new StringWriter();
    }

    [TearDown]
    public void TearDown()
    {
        output.Dispose();
        error.Dispose();
    }

    [Test]
    public async Task ExplicitConsoleReceivesHandlerOutput()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start("write", CreateConsole()), Is.EqualTo(0));
        Assert.That(output.ToString(), Does.Contain("class out"));
        Assert.That(error.ToString(), Does.Contain("class err"));
    }

    [Test]
    public async Task MethodCommandReceivesConsole()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start("echo hello", CreateConsole()), Is.EqualTo(0));
        Assert.That(output.ToString(), Does.Contain("hello"));

        var echo = starter.RootCommand.Subcommands.Single(command => command.Name == "echo");
        Assert.That(echo.Arguments.Select(argument => argument.Name), Is.EqualTo(new[] { "text" }));
    }

    [Test]
    public async Task AppStarterReceivesConsole()
    {
        await using var app = new CmdStarterBuilder()
            .AddCommandsFromAssembly(typeof(Write).Assembly)
            .ConfigureStarter(starter => starter.Namespaces = ImmutableList.Create(typeof(Write).Namespace!))
            .Build();

        // RunAsync has no console parameter: the console goes through the app's Starter.
        Assert.That(await app.Starter.Start("echo hello", CreateConsole()), Is.EqualTo(0));
        Assert.That(output.ToString(), Does.Contain("hello"));

        app.Starter.DefaultConsole = CreateConsole();
        Assert.That(await app.RunAsync("echo again"), Is.EqualTo(0));
        Assert.That(output.ToString(), Does.Contain("again"));
    }

    [Test]
    public async Task ConsoleInputIsAvailable()
    {
        var starter = CreateStarter();
        var console = new CommandConsole(new StringReader("abc"), output, error);

        Assert.That(await starter.Start("read-length", console), Is.EqualTo(3));
    }

    [Test]
    public async Task HelpAndParseErrorsGoToConsole()
    {
        var starter = CreateStarter();
        var processOutput = new StringWriter();
        var processError = new StringWriter();
        var previousOutput = Console.Out;
        var previousError = Console.Error;
        Console.SetOut(processOutput);
        Console.SetError(processError);
        try
        {
            Assert.That(await starter.Start("--help", CreateConsole()), Is.EqualTo(0));
            Assert.That(output.ToString(), Is.Not.Empty);

            Assert.That(await starter.Start("unknown-command", CreateConsole()), Is.Not.EqualTo(0));
            Assert.That(error.ToString(), Is.Not.Empty);
        }
        finally
        {
            Console.SetOut(previousOutput);
            Console.SetError(previousError);
        }

        Assert.That(processOutput.ToString(), Is.Empty);
        Assert.That(processError.ToString(), Is.Empty);
    }

    [Test]
    public async Task HandlerExceptionMessageGoesToConsoleError()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start("throw", CreateConsole()), Is.EqualTo(1));
        Assert.That(error.ToString(), Does.Contain("console failure"));
    }

    [Test]
    public async Task DefaultConsoleIsUsed()
    {
        var starter = CreateStarter();
        starter.DefaultConsole = CreateConsole();

        Assert.That(await starter.Start("write"), Is.EqualTo(0));
        Assert.That(output.ToString(), Does.Contain("class out"));
    }

    [Test]
    public async Task WithoutConsoleProcessConsoleIsReadAtExecution()
    {
        var starter = CreateStarter();
        var processOutput = new StringWriter();
        var previousOutput = Console.Out;
        Console.SetOut(processOutput);
        try
        {
            Assert.That(await starter.Start("write"), Is.EqualTo(0));
        }
        finally
        {
            Console.SetOut(previousOutput);
        }

        Assert.That(processOutput.ToString(), Does.Contain("class out"));
    }

    [Test]
    public async Task ReplLoopUsesDefaultConsole()
    {
        var executed = 0;
        var repl = new ReplStarter(new ScriptedInputProvider("write"))
        {
            Namespaces = ImmutableList.Create(typeof(Write).Namespace!),
            DefaultConsole = CreateConsole()
        };
        repl.OnCommandExecuted += (_, _) => executed++;

        await repl.Launch();

        Assert.That(output.ToString(), Does.Contain("class out"));
        Assert.That(executed, Is.EqualTo(1));
    }

    [Test]
    public async Task FilterSeesExecutionConsole()
    {
        var filter = new ConsoleCapturingFilter();
        var starter = CreateStarter().AddFilter(filter);
        var console = CreateConsole();

        Assert.That(await starter.Start("write", console), Is.EqualTo(0));
        Assert.That(filter.Seen, Is.SameAs(console));
    }

    [Test]
    public async Task ConcurrentExecutionsKeepTheirConsole()
    {
        var starter = CreateStarter();
        starter.InstantiateCommands(); // The tree is built before the executions overlap: building it is not concurrent-safe.
        Rendezvous.Reset(expectedExecutions: 2);
        var firstOutput = new StringWriter();
        var secondOutput = new StringWriter();
        var first = new CommandConsole(new StringReader(string.Empty), firstOutput, new StringWriter());
        var second = new CommandConsole(new StringReader(string.Empty), secondOutput, new StringWriter());

        var codes = await Task.WhenAll(
            Task.Run(() => starter.Start("rendezvous first", first)),
            Task.Run(() => starter.Start("rendezvous second", second)));

        Assert.That(codes, Is.EqualTo(new[] { 0, 0 }));
        Assert.That(firstOutput.ToString(), Does.Contain("hello first").And.Not.Contain("second"));
        Assert.That(secondOutput.ToString(), Does.Contain("hello second").And.Not.Contain("first"));
    }

    [Test]
    public async Task NestedStartKeepsOuterConsole()
    {
        var starter = CreateStarter();
        var innerOutput = new StringWriter();
        NestedStart.CurrentStarter = starter;
        NestedStart.InnerConsole = new CommandConsole(new StringReader(string.Empty), innerOutput, new StringWriter());

        Assert.That(await starter.Start("nested-start", CreateConsole()), Is.EqualTo(0));
        Assert.That(innerOutput.ToString(), Does.Contain("class out"));
        Assert.That(innerOutput.ToString(), Does.Not.Contain("after nested"));
        Assert.That(output.ToString(), Does.Contain("after nested"));
        Assert.That(output.ToString(), Does.Not.Contain("class out"));
    }

    private static Starter CreateStarter() => new()
    {
        Namespaces = ImmutableList.Create(typeof(Write).Namespace!)
    };

    private CommandConsole CreateConsole() => new(new StringReader(string.Empty), output, error);

    private sealed class ConsoleCapturingFilter : ICommandFilter
    {
        public CommandConsole? Seen { get; private set; }

        public Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next)
        {
            Seen = context.Console;
            return next();
        }
    }
}
