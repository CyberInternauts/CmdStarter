namespace com.cyberinternauts.csharp.CmdStarter.Tests
{
    [Category("* All *")]
    [Category("CmdStarter")]
    [Category("Commands")]
    [Category("REPL")]
    public class Repl
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
        public void EnsureExecution()
        {
            Assert.Fail();
        }
    }
}
