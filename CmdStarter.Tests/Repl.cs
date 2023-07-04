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

        [TestCase<CommandOne, CommandTwo, CommandThree>()]
        public void EnsureExecution<CommandOne, CommandTwo, CommandThree>()
            where CommandOne : StarterCommand
            where CommandTwo : StarterCommand
            where CommandThree : StarterCommand
        {
            starter.Namespaces = starter.Namespaces.Add(typeof(CommandOne).Namespace!);
            starter.InstantiateCommands();

            inputProvider.CommandQueue.Enqueue(typeof(CommandOne).Name.PascalToKebabCase());
            inputProvider.CommandQueue.Enqueue(typeof(CommandTwo).Name.PascalToKebabCase());
            inputProvider.CommandQueue.Enqueue(typeof(CommandThree).Name.PascalToKebabCase());


        }
    }
}
