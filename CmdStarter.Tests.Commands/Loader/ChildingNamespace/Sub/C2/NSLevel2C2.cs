using com.cyberinternauts.csharp.CmdStarter.Lib.Loader;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.ChildingNamespace.Sub.C2
{
    [TestParent(ClassesBuildingMode.OnlyNamespaces, typeof(NSLevel1))]
    public class NSLevel2C2 : StarterCommand
    {
    }
}
