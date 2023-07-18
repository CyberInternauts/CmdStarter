using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.TestsCommandsAttributes;

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

        [TestCase<OptionlessCommand, GlobalOptionsFilterOnlyInclude>()]
        [TestCase<OptionlessCommand, GlobalOptionsFilterOnlyExclude>()]
        [TestCase<OptionlessCommand, GlobalOptionFilterExcludeAll>()]
        public void EnsureGlobalOptionsFilter<Command, GlobalContainer>() 
            where Command : IStarterCommand
            where GlobalContainer : IGlobalOptionsContainer, IOptByAttribute
        {
            starter.Classes = starter.Classes.Clear().Add($"~{nameof(MainGlobalOptions)}");
            starter.Classes = starter.Classes.Add($"~{nameof(SecondaryGlobalOptions)}");
            starter.Classes = starter.Classes.Add($"~{nameof(GlobalOptionsFilterOnlyInclude)}");
            starter.Classes = starter.Classes.Add($"~{nameof(GlobalOptionsFilterOnlyExclude)}");
            starter.Classes = starter.Classes.Add($"~{nameof(GlobalOptionFilterExcludeAll)}");

            starter.Classes = starter.Classes.Remove($"~{typeof(GlobalContainer).Name}");

            starter.InstantiateCommands();

            var command = starter.RootCommand;
            
            foreach(var option in GlobalContainer.IncludedOptions)
            {
                var includedOption = command.Options.FirstOrDefault(o =>
                    o.Name == option);

                Assert.That(includedOption, Is.Not.Null);
            }

            foreach (var option in GlobalContainer.ExcludedOptions)
            {
                var excludedOption = command.Options.FirstOrDefault(o =>
                    o.Name == option);

                Assert.That(excludedOption, Is.Null);
            }
        }
    }
}
