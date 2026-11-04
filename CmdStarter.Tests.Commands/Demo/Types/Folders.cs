using CyberInternauts.CmdStarter.Loader;
using CyberInternauts.CmdStarter.Tests.Commands.GlobalOptions;

namespace CyberInternauts.CmdStarter.Tests.Commands.Demo.Types;

[TestParent(ClassesBuildingMode.Both, typeof(List))]
[TestParent(ClassesBuildingMode.OnlyAttributes, typeof(List))]
[TestParent(ClassesBuildingMode.OnlyNamespaces, typeof(List))]
[Parent<List>]
public class Folders : StarterCommand
{
    public override Delegate HandlingMethod => Execute;

    private void Execute()
    {
        var globalInt = GetGlobalOptions<MainGlobalOptions>()?.IntGlobalOption;
        Console.WriteLine("globalInt=" + globalInt);
    }
}