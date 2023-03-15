using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel;
using System.CommandLine.NamingConventionBinder;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Listing.Types
{
    [Description("List only the files of a folder")]
    [TestParent(ClassesBuildingMode.Both, typeof(List))]
    [TestParent(ClassesBuildingMode.OnlyAttributes, null)]
    [TestParent(ClassesBuildingMode.OnlyNamespaces, typeof(List))]
    public class Files : StarterCommand
    {
        [Description("Option description")]
        public int MyOpt { get; set; } = 987;

        //TODO: Make a generic version that resides in the base class|interface
        protected void HandleCommandOptions(Files me)
        {
            //TODO: Copy all properties
            this.MyOpt = me.MyOpt;
            var a = 1;
        }

        protected int HandleCommand(InvocationContext context)
        {
            CommandHandler.Create(HandleCommandOptions).Invoke(context);
            return CommandHandler.Create(MethodForHandling).Invoke(context); //TODO: Manage async
        }

        Delegate MethodForHandling { get => HandleInvoke; }

        private void HandleInvoke([Description("Param1")] string path)
        {
            // Here the properties are filled from main args
            var a = 1;
            Console.WriteLine("path=" + path + "; MyOpt=" + MyOpt);
        }

        public Files()
        {
            var parameters = MethodForHandling.Method.GetParameters();
            //TODO: Build arguments using handling method params
            var at = typeof(Argument<>).MakeGenericType(typeof(string)); //TODO: typeof shall use the parameter type of the handling method
            var ctor = at.GetConstructor(Type.EmptyTypes);
            var ata = (Argument)ctor!.Invoke(null); 
            ata.Name = "path"; //TODO: "path" must match the parameter name of the handling method
            ata.Description = "get desc from attribute1";
            this.AddArgument(ata);

            //TODO : Build options using class properties
            var opt = new Option<int>("--my-opt", "get desc from attribute2");
            opt.IsRequired = false;
            this.AddOption(opt);

            Handler = CommandHandler.Create(HandleCommand);
        }
    }
}