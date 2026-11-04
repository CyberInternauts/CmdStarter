using CyberInternauts.CmdStarter.Tests.Commands.GlobalOptions;
using CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;

namespace CyberInternauts.CmdStarter.Tests.Commands.Methods;

// Every parameter below matches two classification rules: the rule checked first decides what the parameter becomes.
public static class ParameterOrderCommands
{
    [Command]
    public static int Check(
        [Option] CancellationToken token,
        [Option] CommandConsole console,
        [Option] MainGlobalOptions globals,
        [FromServices][Option] SingletonService fromServices,
        [Option] ScopedService option,
        ScopedConsumer inferred,
        string text)
    {
        _ = (token, console, globals, fromServices, option, inferred, text);
        return 0;
    }
}
