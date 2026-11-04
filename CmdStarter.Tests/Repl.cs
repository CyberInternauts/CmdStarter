using CyberInternauts.CmdStarter.Tests.Common.TestsCommandsAttributes;
using CyberInternauts.CmdStarter.Tests.Commands.Repl;
using CyberInternauts.CmdStarter.Repl;
using CyberInternauts.CmdStarter.Tests.Commands.Loader.DepencendyInjection;
using CyberInternauts.CmdStarter.Tests.Common.Interfaces;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("CmdStarter")]
[Category("Commands")]
[Category("REPL")]
public class Repl
{
    public static ReplStarter starter;
    private TestInputProvider inputProvider;

    [OneTimeSetUp]
    public void GlobalSetup()
    {
        TestsCommon.GlobalSetup();
    }

    [SetUp]
    public void MethodSetup()
    {
        inputProvider = new TestInputProvider();
        starter = new ReplStarter(inputProvider); // Reset object to a new one, not to interfer between tests
        SetDefaultNamespaces(starter);
    }

    public static void SetDefaultNamespaces(CmdStarter.Starter starter)
    {
        starter.Namespaces = starter.Namespaces.Clear();
        if (TestsCommon.ERRONEOUS_NAMESPACE != string.Empty) // Remove, by default, erroneous namespace
        {
            starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + TestsCommon.ERRONEOUS_NAMESPACE);
        }
        // Remove, by default, dependency injection namespace
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Dependent1).Namespace!);
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Commands.Lifecycle.LifecycleLog).Namespace!);
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Commands.Methods.UserCommands).Namespace!);
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Commands.Cancellation.CancellableCommand).Namespace!);
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Commands.ReplApp.ReplAppCommands).Namespace!);
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Commands.States.Enter).Namespace!);
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Commands.Validation.LevelCommand).Namespace!);
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Commands.MethodGroups.AdminCommands).Namespace!);
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Commands.Hosting.LifecycleRecorder).Namespace!);
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Commands.Erroneous.MethodGroups.DuplicatePathA).Namespace!);
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + "CyberInternauts.CmdStarter.Tests.Commands.Erroneous.NameConflicts");
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + "CyberInternauts.CmdStarter.Tests.Commands.SameLeafNames");
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + "CyberInternauts.CmdStarter.Tests.Commands.DefaultValues");
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + "CyberInternauts.CmdStarter.Tests.Commands.FluentTree");
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + "CyberInternauts.CmdStarter.Tests.Commands.NamespaceGroups");
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + "CyberInternauts.CmdStarter.Tests.Commands.Filters");
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Commands.Consoles.Write).Namespace!);
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Commands.Sequences.Ok).Namespace!);
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Commands.Remote.Hello).Namespace!);
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Commands.PrivateSetters.PrivateSetterOptions).Namespace!);
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Commands.BrokenGlobalOptions.HealthyCommand).Namespace!);
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Commands.ExecutionPaths.ExecutionLog).Namespace!);
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + "CyberInternauts.CmdStarter.Tests.Commands.TreeBuilding");
        starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Commands.Catalog.Listed).Namespace!);
    }

    [TestCase<PreCommand, CommandOne, CommandTwo, CommandThree>()]
    public async Task EnsureExecutionWithStringPreCommand<PreCommand, CommandOne, CommandTwo, CommandThree>()
        where PreCommand : StarterCommand, IHasExpectedValue<int>
        where CommandOne : StarterCommand, IHasExpectedValue<int>
        where CommandTwo : StarterCommand, IHasExpectedValue<int>
        where CommandThree : StarterCommand, IHasExpectedValue<int>
    {
        var executionQueue = new Queue<IHasExpectedValue<int>>();

        starter.Namespaces = starter.Namespaces.Add(typeof(CommandOne).Namespace!);
        starter.InstantiateCommands();

        starter.OnCommandExecuted += (object? sender, ReplCommandEventArgs eventArgs) =>
        {
            if (!executionQueue.TryDequeue(out var command))
            {
                starter.Stop();
                return;
            }

            var actualReturnCode = eventArgs.ReturnCode;

            Assert.That(actualReturnCode, Is.EqualTo(command.ExpectedValue));
        };

        var preCommand = starter.FindCommandInstance<PreCommand>()!;
        var commandOne = starter.FindCommandInstance<CommandOne>()!;
        var commandTwo = starter.FindCommandInstance<CommandTwo>()!;
        var commandThree = starter.FindCommandInstance<CommandThree>()!;

        executionQueue.Enqueue(preCommand);
        executionQueue.Enqueue(commandOne);
        executionQueue.Enqueue(commandTwo);
        executionQueue.Enqueue(commandThree);

        inputProvider.CommandQueue.Enqueue(commandOne.Name);
        inputProvider.CommandQueue.Enqueue(commandTwo.Name);
        inputProvider.CommandQueue.Enqueue(commandThree.Name);

        var args = preCommand.Name;

        await starter.Launch(args);
    }

    [TestCase<PreCommand, CommandOne, CommandTwo, CommandThree>()]
    public async Task EnsureExecutionWithArrayPreCommand<PreCommand, CommandOne, CommandTwo, CommandThree>()
        where PreCommand : StarterCommand, IHasExpectedValue<int>
        where CommandOne : StarterCommand, IHasExpectedValue<int>
        where CommandTwo : StarterCommand, IHasExpectedValue<int>
        where CommandThree : StarterCommand, IHasExpectedValue<int>
    {
        var executionQueue = new Queue<IHasExpectedValue<int>>();

        starter.Namespaces = starter.Namespaces.Add(typeof(CommandOne).Namespace!);
        starter.InstantiateCommands();

        starter.OnCommandExecuted += (object? sender, ReplCommandEventArgs eventArgs) =>
        {
            if (!executionQueue.TryDequeue(out var command))
            {
                starter.Stop();
                return;
            }

            var actualReturnCode = eventArgs.ReturnCode;

            Assert.That(actualReturnCode, Is.EqualTo(command.ExpectedValue));
        };

        var preCommand = starter.FindCommandInstance<PreCommand>()!;
        var commandOne = starter.FindCommandInstance<CommandOne>()!;
        var commandTwo = starter.FindCommandInstance<CommandTwo>()!;
        var commandThree = starter.FindCommandInstance<CommandThree>()!;

        executionQueue.Enqueue(preCommand);
        executionQueue.Enqueue(commandOne);
        executionQueue.Enqueue(commandTwo);
        executionQueue.Enqueue(commandThree);

        inputProvider.CommandQueue.Enqueue(commandOne.Name);
        inputProvider.CommandQueue.Enqueue(commandTwo.Name);
        inputProvider.CommandQueue.Enqueue(commandThree.Name);

        var args = new string[] { preCommand.Name };

        await starter.Launch(args);
    }

    [TestCase<CommandOne, CommandTwo, CommandThree>()]
    public async Task EnsureExecution<CommandOne, CommandTwo, CommandThree>()
        where CommandOne : StarterCommand, IHasExpectedValue<int>
        where CommandTwo : StarterCommand, IHasExpectedValue<int>
        where CommandThree : StarterCommand, IHasExpectedValue<int>
    {
        var executionQueue = new Queue<IHasExpectedValue<int>>();

        starter.Namespaces = starter.Namespaces.Add(typeof(CommandOne).Namespace!);
        starter.InstantiateCommands();

        starter.OnCommandExecuted += (object? sender, ReplCommandEventArgs eventArgs) =>
        {
            if (!executionQueue.TryDequeue(out var command))
            {
                starter.Stop();
                return;
            }

            var actualReturnCode = eventArgs.ReturnCode;

            Assert.That(actualReturnCode, Is.EqualTo(command.ExpectedValue));
        };

        var commandOne = starter.FindCommandInstance<CommandOne>()!;
        var commandTwo = starter.FindCommandInstance<CommandTwo>()!;
        var commandThree = starter.FindCommandInstance<CommandThree>()!;

        executionQueue.Enqueue(commandOne);
        executionQueue.Enqueue(commandTwo);
        executionQueue.Enqueue(commandThree);

        inputProvider.CommandQueue.Enqueue(commandOne.Name);
        inputProvider.CommandQueue.Enqueue(commandTwo.Name);
        inputProvider.CommandQueue.Enqueue(commandThree.Name);

        await starter.Launch();
    }

    [Test]
    public async Task EndsLoopAtEndOfInput()
    {
        var codes = new List<int>();
        var repl = CreateScriptedStarter(codes, "command-one");

        await repl.Launch().WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(codes, Is.EqualTo(new[] { CommandOne.EXPECTED_RETURN }));
    }

    [Test]
    public async Task SkipsEmptyLines()
    {
        var codes = new List<int>();
        var repl = CreateScriptedStarter(codes, "", "   ", "command-one");

        await repl.Launch().WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(codes, Is.EqualTo(new[] { CommandOne.EXPECTED_RETURN }));
    }

    [TestCase("exit")]
    [TestCase("QUIT")]
    public async Task ExitCommandEndsLoop(string exitCommand)
    {
        var codes = new List<int>();
        var repl = CreateScriptedStarter(codes, "command-one", exitCommand, "command-two");

        await repl.Launch().WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(codes, Is.EqualTo(new[] { CommandOne.EXPECTED_RETURN }));
    }

    [Test]
    public async Task ExitCommandsCanBeDisabled()
    {
        var codes = new List<int>();
        var repl = CreateScriptedStarter(codes, "exit");
        repl.ExitCommands = System.Collections.Immutable.ImmutableList<string>.Empty;

        await repl.Launch().WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(codes, Has.Count.EqualTo(1));
        Assert.That(codes[0], Is.Not.EqualTo(0));
    }

    [TestCase("help")]
    [TestCase("help command-one")]
    public async Task HelpCommandShowsHelp(string line)
    {
        var codes = new List<int>();
        var repl = CreateScriptedStarter(codes, line);

        await repl.Launch().WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(codes, Is.EqualTo(new[] { 0 }));
    }

    [Test]
    public async Task HelpCommandsCanBeDisabled()
    {
        var codes = new List<int>();
        var repl = CreateScriptedStarter(codes, "help");
        repl.HelpCommands = System.Collections.Immutable.ImmutableList<string>.Empty;

        await repl.Launch().WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(codes, Has.Count.EqualTo(1));
        Assert.That(codes[0], Is.Not.EqualTo(0));
    }

    [Test]
    public async Task LaunchEndsWhenCancelled()
    {
        using var cts = new CancellationTokenSource();
        var repl = new ReplStarter(new BlockingInputProvider());
        var run = repl.Launch(cts.Token);
        cts.Cancel();

        Assert.DoesNotThrowAsync(async () => await run.WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [Test]
    public async Task StopEndsTheLoop()
    {
        var codes = new List<int>();
        var repl = CreateScriptedStarter(codes, "command-one", "command-two");
        repl.OnCommandExecuted += (_, _) => repl.Stop();

        await repl.Launch().WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(codes, Is.EqualTo(new[] { CommandOne.EXPECTED_RETURN }));
    }

    [Test]
    public async Task CancelCurrentCommandCancelsRunningCommand()
    {
        var codes = new List<int>();
        var repl = new ReplStarter(new ScriptedInputProvider("wait-for-cancel"));
        repl.Namespaces = System.Collections.Immutable.ImmutableList.Create(typeof(Commands.Cancellation.CancellableMethods).Namespace!);
        repl.Classes = System.Collections.Immutable.ImmutableList.Create(typeof(Commands.Cancellation.CancellableMethods).FullName!);
        repl.OnCommandExecuted += (_, eventArgs) => codes.Add(eventArgs.ReturnCode);
        Commands.Cancellation.CancellableMethods.Reset();

        var run = repl.Launch();
        await Commands.Cancellation.CancellableMethods.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(repl.CancelCurrentCommand(), Is.True);
        await run.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.That(codes, Is.EqualTo(new[] { 130 }));
        Assert.That(repl.CancelCurrentCommand(), Is.False);
    }

    [Test]
    public void CancelCurrentCommandReturnsFalseWhenIdle()
    {
        var repl = new ReplStarter(new ScriptedInputProvider());

        Assert.That(repl.CancelCurrentCommand(), Is.False);
    }

    private static ReplStarter CreateScriptedStarter(List<int> codes, params string?[] lines)
    {
        var repl = new ReplStarter(new ScriptedInputProvider(lines));
        SetDefaultNamespaces(repl);
        repl.Namespaces = repl.Namespaces.Add(typeof(CommandOne).Namespace!);
        repl.OnCommandExecuted += (_, eventArgs) => codes.Add(eventArgs.ReturnCode);
        return repl;
    }
}
