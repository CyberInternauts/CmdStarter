using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Tests.Commands.ExecutionPaths;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("CmdStarter")]
public class HandlerParameters
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [Test]
    public async Task HandlerParametersWithoutNameAreNotArgumentsAndReceiveTheDefaultOfTheirType()
    {
        var starter = TestsCommon.CreateCmdStarter();
        starter.CommandsTypes = [typeof(NamelessParameterCommand)];
        using var error = new StringWriter();
        var console = new CommandConsole(new StringReader(string.Empty), new StringWriter(), error);

        var code = await starter.Start([], console);

        // 0 for the int and null for the string: the handler returns 0 + 2.
        Assert.That(code, Is.EqualTo(2), error.ToString());
        Assert.That(starter.RootCommand.Arguments, Is.Empty);
    }
}
