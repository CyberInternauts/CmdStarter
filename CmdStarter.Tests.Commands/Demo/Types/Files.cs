using com.cyberinternauts.csharp.CmdStarter.Lib.Loader;
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
        public override Delegate HandlingMethod => Execute;

        public string FilesPattern { get; set; } = "";
        public const string FILES_PATTERN_NAME = "files-pattern";

        private void Execute([Description("Folder to list files")] string folder)
        {
            Console.WriteLine("Should list " + folder + (FilesPattern != string.Empty ? " with \"" + FilesPattern + "\"" : string.Empty));
        }
    }
}