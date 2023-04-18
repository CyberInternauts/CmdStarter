namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.ChildingNamespace
{
    [TestParent(ClassesBuildingMode.OnlyNamespaces, typeof(Main))]
    public class NSLevel1 : StarterCommand
    {
    }
}
