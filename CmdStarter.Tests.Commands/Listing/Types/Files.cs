using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel;
using System.CommandLine.NamingConventionBinder;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions;

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

        public DirectoryInfo? Folder { get; set; }

        public List<int> MultipleOption { get; set; } = new();

        //TODO: Make a generic version that resides in the base class|interface
        protected void HandleCommandOptions<T>(InvocationContext context, T me)
        {
            /*
            //TODO: Copy all properties
            this.MyOpt = me.MyOpt;
            this.MultipleOption = me.MultipleOption;

            // Fill AllowMultiple options ==> NOT NEEDED. I had made a mistake because I didn't change the option to List<int> instead of int. Now, the list is filled
            var results = context.ParseResult.FindResultFor(this.Options[1]);
            var values = results!.Tokens.Select(t => t.Value);
            foreach (var value in values)
            {
                dynamic convertedValue = Convert.ChangeType(value, typeof(int));
                this.MultipleOption.Add(convertedValue);
            }
            */
            var a = 1;
        }

        private AllGlobalOptions? globalOptions;

        private void HandleGlobalOptions<T>(InvocationContext context, T globalOptions)
        {
            var currentCommand = (Files)context.BindingContext.ParseResult.CommandResult.Command!;
            currentCommand.globalOptions = globalOptions as AllGlobalOptions;
        }

        protected int HandleCommand(InvocationContext context)
        {
            var hgo = this.GetType()
                .GetMethod(nameof(HandleGlobalOptions), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(typeof(AllGlobalOptions));
            CommandHandler.Create(hgo).Invoke(context);


            var c = this.GetType()
                .GetMethod(nameof(HandleCommandOptions), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(typeof(Files));

            //context.ParseResult.FindResultFor(this.Options[1]).Tokens
            CommandHandler.Create(c).Invoke(context);
            return CommandHandler.Create(MethodForHandling).Invoke(context); //TODO: Manage async
        }

        protected override Delegate MethodForHandling { get => HandleInvoke; }

        private void HandleInvoke([Description("Param1")] string path = "default_path")
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
            ata.SetDefaultValue("default_path");
            this.AddArgument(ata);

            //TODO : Build options using class properties
            var opt = new Option<int>("--my-opt");
            opt.Description = "get desc from attribute2"; //TODO: Take from attribute
            opt.IsRequired = false; //TODO: Take from attribute
            opt.AllowMultipleArgumentsPerToken = false; //TODO: Set to true if type implements IList
            opt.AddAlias("-mo"); //TODO: Take from attribute
            this.AddOption(opt);

            //Test for AllowMultiple true
            //Usage: CmdStarter.Tester.exe list files mypath -mm 321 -mm 456
            var optMultiple = new Option<List<int>>("--multiple-option");
            optMultiple.Description = "get desc from attribute3"; //TODO: Take from attribute
            optMultiple.IsRequired = false; //TODO: Take from attribute
            optMultiple.AllowMultipleArgumentsPerToken = true; //TODO: Set to true if type implements IList
            optMultiple.AddAlias("-mm"); //TODO: Take from attribute
            this.AddOption(optMultiple);

            // Test for DirectoryInfo
            var optFolder = new Option<DirectoryInfo>("--folder");
            this.AddOption(optFolder);

            Handler = CommandHandler.Create(HandleCommand);
        }
    }
}