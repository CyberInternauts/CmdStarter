using CyberInternauts.CmdStarter.Tests.Commands.GlobalOptions;
using CyberInternauts.CmdStarter.Tests.Commands.Methods;

namespace CyberInternauts.CmdStarter.Tests;

/// <summary>A method command that takes a global options container needs the filters to keep that container.</summary>
[Category("* All *")]
[Category("Methods")]
[Category("GlobalOptions")]
public class MethodGlobalOptionsFilters
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [Test]
    public void ContainerParameterThatTheFiltersExcludeIsAConstructionError()
    {
        // ToolCommands.Status takes a MainGlobalOptions, which this filter does not include.
        var starter = CreateStarter(typeof(ToolCommands));

        var exception = Assert.Throws<InvalidAttributeException>(starter.InstantiateCommands);

        Assert.That(exception!.Message, Does.Contain("'globals'").And.Contain("'Status'").And.Contain(typeof(ToolCommands).FullName).And.Contain(typeof(MainGlobalOptions).FullName));
    }

    [Test]
    public async Task ContainerParameterThatTheFiltersKeepWorksFromTheFirstConstruction()
    {
        var starter = CreateStarter(typeof(ToolCommands), typeof(MainGlobalOptions));

        Assert.DoesNotThrow(starter.InstantiateCommands);
        Assert.That(await starter.Start("status --int-global-option 5"), Is.EqualTo(5));
    }

    private static Starter CreateStarter(params Type[] types)
    {
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Clear();
        starter.Classes = starter.Classes.Clear().AddRange(types.Select(type => type.FullName!));
        return starter;
    }
}
