using CyberInternauts.CmdStarter;
using CyberInternauts.CmdStarter.Tests.Commands.Commands;
using CyberInternauts.CmdStarter.Tests.Commands.GlobalOptions;
using CyberInternauts.CmdStarter.Tests.Commands.GlobalOptions.Filters;
using CyberInternauts.CmdStarter.Tests.Common.Interfaces;
using CyberInternauts.CmdStarter.Tests.Common.TestsCommandsAttributes;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("CmdStarter")]
[Category("Commands")]
[Category("Options")]
[Category("GlobalOptions")]
public class GlobalOptions
{
    private Starter starter;

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

        var mainOption = TestsCommon.FindOption(starter.RootCommand, MainGlobalOptions.INT_GLOBAL_OPTION_KEBAB);
        Assert.That(mainOption, Is.Not.Null);

        var secondaryOption = TestsCommon.FindOption(starter.RootCommand, SecondaryGlobalOptions.SECONDARY_INT_GLOBAL_OPTION_KEBAB);
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

        foreach (var option in GlobalContainer.IncludedOptions)
        {
            var includedOption = TestsCommon.FindOption(rootCommand, option);

            Assert.That(includedOption, Is.Not.Null);
        }

        foreach (var option in GlobalContainer.ExcludedOptions)
        {
            var excludedOption = TestsCommon.FindOption(rootCommand, option);

            Assert.That(excludedOption, Is.Null);
        }
    }
}
