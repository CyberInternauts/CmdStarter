using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Demo.Types
{
    [TestParent(ClassesBuildingMode.Both, typeof(List))]
    [TestParent(ClassesBuildingMode.OnlyAttributes, typeof(List))]
    [TestParent(ClassesBuildingMode.OnlyNamespaces, typeof(List))]
    [Parent<List>]
    public class Folders : StarterCommand
    {
        public override Delegate MethodForHandling => Execute;

        private void Execute()
        {
            var globalInt = GetGlobalOptions<MainGlobalOptions>()?.IntGlobalOption;
            var a = 1;
        }
    }
}