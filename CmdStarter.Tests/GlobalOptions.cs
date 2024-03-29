﻿using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Commands;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions.Filters;
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

        [TestCase<GlobalOptionsFilterOnlyInclude>()]
        [TestCase<GlobalOptionFilterOnlyExclude>()]
        [TestCase<GlobalOptionFilterExcludeAll>()]
        [TestCase<GlobalOptionFilterByLackOfAttribute>()]
        public void EnsureGlobalOptionsFilter<GlobalContainer>() 
            where GlobalContainer : IGlobalOptionsContainer, IOptByAttribute
        {
            starter.Classes = starter.Classes.Clear().Add(typeof(GlobalContainer).FullName!);
            starter.Classes = starter.Classes.Add(nameof(EmptyCommand));

            starter.InstantiateCommands();

            var rootCommand = starter.RootCommand;
            
            foreach(var option in GlobalContainer.IncludedOptions)
            {
                var includedOption = rootCommand.Options.FirstOrDefault(o =>
                    o.Name == option);

                Assert.That(includedOption, Is.Not.Null);
            }

            foreach (var option in GlobalContainer.ExcludedOptions)
            {
                var excludedOption = rootCommand.Options.FirstOrDefault(o =>
                    o.Name == option);

                Assert.That(excludedOption, Is.Null);
            }
        }
    }
}
