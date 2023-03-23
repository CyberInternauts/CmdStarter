namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Listing.Types
{
    [TestParent(ClassesBuildingMode.Both, typeof(List))]
    [TestParent(ClassesBuildingMode.OnlyAttributes, typeof(List))]
    [TestParent(ClassesBuildingMode.OnlyNamespaces, typeof(List))]
    [Parent<List>]
    public class Folders : StarterCommand
    {
    }
}