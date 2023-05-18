using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments.Child;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.TestsCommandsAttributes;
using static com.cyberinternauts.csharp.CmdStarter.Tests.TestsCommon;

namespace com.cyberinternauts.csharp.CmdStarter.Tests
{
    [Category("* All *")]
    [Category("CmdStarter")]
    [Category("Commands")]
    [Category("Arguments")]
    public class Arguments
    {
        private Lib.Starter starter;

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            TestsCommon.GlobalSetup();
        }

        [SetUp]
        public void MethodSetup()
        {
            starter = TestsCommon.CreateCmdStarter();
        }

        [TestCase<FullArgs>]
        [TestCase<NoArgs>]
        [TestCase<ArgParent>]
        [TestCase<ArgChild>]
        [TestCase<FullArgsByInterface>]
        public void EnsuresArgumentsAreProperlyCreated<CommandType>() where CommandType : IStarterCommand
        {
            // Using NoArgs instead of CommandType, otherwise with ArgChild, it finds only one command and now the behavior is to root options/arguments
            starter.Namespaces = starter.Namespaces.Clear().Add(typeof(NoArgs).Namespace!);
            starter.InstantiateCommands();

            var command = starter.FindCommand<CommandType>() as StarterCommand;
            Assert.That(command, Is.Not.Null);

            if (command.Subcommands.Count > 0) // Not a leaf, arguments can't be used
            {
                Assert.That(command.Arguments, Has.Count.EqualTo(0));
                return;
            }

            AssertArguments(command.UnderlyingCommand, command);
        }
    }
}
