using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel;
using System.CommandLine.NamingConventionBinder;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Listing.Types
{
    [Description("List only the files of a folder")]
    [TestParent(ClassesBuildingMode.Both, typeof(List))]
    [TestParent(ClassesBuildingMode.OnlyAttributes, null)]
    [TestParent(ClassesBuildingMode.OnlyNamespaces, typeof(List))]
    public class Files : StarterCommand
    {
        public int MyOpt { get; set; } = 987;

        public Files()
        {
            var arg = new Argument<string>("path");
            this.AddArgument(arg);
            var opt = new Option<int>("--my-opt");
            opt.IsRequired = false;
            this.AddOption(opt);

            Handler = CommandHandler.Create<Files, string>(
                (Files me, string path) =>
                {
                    Console.WriteLine("folderPath=" + path);
                }
            );
        }
    }
}