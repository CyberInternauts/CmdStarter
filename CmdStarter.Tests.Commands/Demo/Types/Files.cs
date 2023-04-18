using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Demo;
using System.ComponentModel;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Demo.Types
{
    [Description("List only the files of a folder")]
    [TestParent(ClassesBuildingMode.Both, typeof(List))]
    [TestParent(ClassesBuildingMode.OnlyAttributes, null)]
    [TestParent(ClassesBuildingMode.OnlyNamespaces, typeof(List))]
    public class Files : StarterCommand
    {
        public override Delegate MethodForHandling => Execute;

        private void Execute([Description("Folder to list files")] string folder)
        {
            Console.WriteLine("Should list " + folder);
        }
    }
}