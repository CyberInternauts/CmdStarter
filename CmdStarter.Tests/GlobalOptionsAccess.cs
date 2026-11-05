using CyberInternauts.CmdStarter.Tests.Commands.GlobalOptions;
using CyberInternauts.CmdStarter.Tests.Commands.Validation;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("GlobalOptions")]
public class GlobalOptionsAccess
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [Test]
    public async Task ContainerIsFoundOnceACommandRanAndOnlyWhenTheFiltersKeptIt()
    {
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Clear().Add(typeof(LevelCommand).Namespace!);
        var manager = starter.GlobalOptionsManager;

        // Nothing is parsed before the first command.
        Assert.That(manager.GetGlobalOptions<LimitOptions>(), Is.Null);

        Assert.That(await starter.Start("level-command --verbosity 2"), Is.EqualTo(5));

        Assert.That(manager.GetGlobalOptions<LimitOptions>()!.Verbosity, Is.EqualTo(2));
        // A container of a namespace the filters left out is never built.
        Assert.That(manager.GetGlobalOptions<MainGlobalOptions>(), Is.Null);
    }
}
