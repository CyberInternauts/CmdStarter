using com.cyberinternauts.csharp.CmdStarter.Tests.Common.TestsCommandsAttributes;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Repl;
using com.cyberinternauts.csharp.CmdStarter.Lib.Repl;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.DepencendyInjection;
using com.cyberinternauts.csharp.CmdStarter.Lib.Extensions;

namespace com.cyberinternauts.csharp.CmdStarter.Tests
{
    [Category("* All *")]
    [Category("CmdStarter")]
    [Category("Commands")]
    [Category("REPL")]
    public class Repl
    {
        public static ReplStarter starter;
        private TestInputProvider inputProvider = new();

        private Queue<int> ensureExecutionTestReturnCodeQueue = new Queue<int>();

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            TestsCommon.GlobalSetup();
        }

        [SetUp]
        public void MethodSetup()
        {
            Starter.SetDefaultFactory();

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

        [TestCase<CommandOne, CommandTwo, CommandThree>(CommandOne.EXPECTED_RETURN, CommandTwo.EXPECTED_RETURN, CommandThree.EXPECTED_RETURN)]
        public async Task EnsureExecution<CommandOne, CommandTwo, CommandThree>(
            int expectedValueOne, int expectedValueTwo, int expectedValueThree)
            where CommandOne : StarterCommand
            where CommandTwo : StarterCommand
            where CommandThree : StarterCommand
        {
            starter.Namespaces = starter.Namespaces.Add(typeof(CommandOne).Namespace!);
            starter.InstantiateCommands();

            starter.OnCommandExecuted += Starter_OnCommandExecuted_Assert;

            ensureExecutionTestReturnCodeQueue.Clear();
            ensureExecutionTestReturnCodeQueue.Enqueue(expectedValueOne);
            ensureExecutionTestReturnCodeQueue.Enqueue(expectedValueTwo);
            ensureExecutionTestReturnCodeQueue.Enqueue(expectedValueThree);

            inputProvider.CommandQueue.Enqueue(typeof(CommandOne).Name.PascalToKebabCase());
            inputProvider.CommandQueue.Enqueue(typeof(CommandTwo).Name.PascalToKebabCase());
            inputProvider.CommandQueue.Enqueue(typeof(CommandThree).Name.PascalToKebabCase());

            await starter.Launch();
        }

        private void Starter_OnCommandExecuted_Assert(object? sender, ReplCommandEventArgs e)
        {
            if(!ensureExecutionTestReturnCodeQueue.TryDequeue(out int expectedReturnCode))
            {
                starter.Stop();
                return;
            }

            var actualReturnCode = e.ReturnCode;

            Assert.That(actualReturnCode, Is.EqualTo(expectedReturnCode));
        }
    }
}
