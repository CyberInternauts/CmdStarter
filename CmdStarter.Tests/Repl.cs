using com.cyberinternauts.csharp.CmdStarter.Tests.Common.TestsCommandsAttributes;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Repl;
using com.cyberinternauts.csharp.CmdStarter.Lib.Repl;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.DepencendyInjection;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests
{
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
            Starter.SetDefaultFactory();

            inputProvider = new TestInputProvider();
            starter = new ReplStarter(inputProvider); // Reset object to a new one, not to interfer between tests
            SetDefaultNamespaces(starter);
        }

        public static void SetDefaultNamespaces(CmdStarter.Lib.Starter starter)
        {
            starter.Namespaces = starter.Namespaces.Clear();
            if (TestsCommon.ERRONEOUS_NAMESPACE != string.Empty) // Remove, by default, erroneous namespace
            {
                starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + TestsCommon.ERRONEOUS_NAMESPACE);
            }
            // Remove, by default, dependency injection namespace
            starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Dependent1).Namespace!);
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

            var preCommand = (PreCommand)starter.FindCommand<PreCommand>()!;
            var commandOne = (CommandOne)starter.FindCommand<CommandOne>()!;
            var commandTwo = (CommandTwo)starter.FindCommand<CommandTwo>()!;
            var commandThree = (CommandThree)starter.FindCommand<CommandThree>()!;

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
                if(!executionQueue.TryDequeue(out var command))
                {
                    starter.Stop();
                    return;
                }

                var actualReturnCode = eventArgs.ReturnCode;

                Assert.That(actualReturnCode, Is.EqualTo(command.ExpectedValue));
            };

            var preCommand = (PreCommand)starter.FindCommand<PreCommand>()!;
            var commandOne = (CommandOne)starter.FindCommand<CommandOne>()!;
            var commandTwo = (CommandTwo)starter.FindCommand<CommandTwo>()!;
            var commandThree = (CommandThree)starter.FindCommand<CommandThree>()!;

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

            var commandOne = (CommandOne)starter.FindCommand<CommandOne>()!;
            var commandTwo = (CommandTwo)starter.FindCommand<CommandTwo>()!;
            var commandThree = (CommandThree)starter.FindCommand<CommandThree>()!;

            executionQueue.Enqueue(commandOne);
            executionQueue.Enqueue(commandTwo);
            executionQueue.Enqueue(commandThree);

            inputProvider.CommandQueue.Enqueue(commandOne.Name);
            inputProvider.CommandQueue.Enqueue(commandTwo.Name);
            inputProvider.CommandQueue.Enqueue(commandThree.Name);

            await starter.Launch();
        }
    }
}
