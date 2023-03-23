namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Listing
{
    [TestParent(ClassesBuildingMode.Both, null)]
    [TestParent(ClassesBuildingMode.OnlyAttributes, null)]
    [TestParent(ClassesBuildingMode.OnlyNamespaces, typeof(Main))]
    [Parent(null)]
    public class List : StarterCommand
    {
    }
}