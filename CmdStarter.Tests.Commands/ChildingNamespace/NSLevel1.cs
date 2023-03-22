namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.ChildingNamespace
{
    [TestParent(ClassesBuildingMode.OnlyNamespaces, typeof(Main))]
    public class NSLevel1 : StarterCommand<NSLevel1>
    {
    }
}
