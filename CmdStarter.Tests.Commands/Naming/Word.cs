namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Naming
{
    [TestParent(ClassesBuildingMode.Both, typeof(Main))]
    [TestParent(ClassesBuildingMode.OnlyAttributes, null)]
    [TestParent(ClassesBuildingMode.OnlyNamespaces, typeof(Main))]
    public class Word : StarterCommand
    {
    }
}
