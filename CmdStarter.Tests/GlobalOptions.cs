using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions;
using System.CommandLine;

namespace com.cyberinternauts.csharp.CmdStarter.Tests
{
    [Category("* All *")]
    [Category("CmdStarter")]
    [Category("Commands")]
    [Category("Options")]
    [Category("GlobalOptions")]
    public class GlobalOptions
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

        [Test]
        public void EnsuresGlobalOptionsPresence()
        {
            starter.InstantiateCommands();

            var mainOption = starter.RootCommand.Options.FirstOrDefault(o => o.Name == MainGlobalOptions.INT_GLOBAL_OPTION_KEBAB);
            Assert.That(mainOption, Is.Not.Null);

            var secondaryOption = starter.RootCommand.Options.FirstOrDefault(o => o.Name == SecondaryGlobalOptions.SECONDARY_INT_GLOBAL_OPTION_KEBAB);
            Assert.That(secondaryOption, Is.Not.Null);
        }

        [Test]
        public void FailingTest()
        {
            Assert.Fail("TEST FAILING");
        }
    }
}
