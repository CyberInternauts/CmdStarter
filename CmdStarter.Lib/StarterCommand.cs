using System.Text;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.ComponentModel;
using System.CommandLine.Invocation;
using com.cyberinternauts.csharp.CmdStarter.Lib.Extensions;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;
using static com.cyberinternauts.csharp.CmdStarter.Lib.Reflection.Helper;
using static com.cyberinternauts.csharp.CmdStarter.Lib.Reflection.CommandLineHelper;
using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Lib
{ 
    //TODO: Is it the best idea to use a Class instead of an Interface? Have both: ...
    ///     - When Interface, and not a StarterCommand, create one using a new sealed class in the Lib with the ClassFound as parameter
    ///         - It will redirect the Handle method from the ClassFound
    ///         - Copy attributes
    ///     - Detect Options with OptionAttribute on public Property|Field
    /// 
    public abstract class StarterCommand : Command
    {
        public const string OPTION_PREFIX = "--";
        public const string DESCRIPTION_JOINER = "\n";

        private const string TEMPORARY_NAME = "temp";


        //TODO: Transfer to IStarterCommand what's below
        public virtual Delegate MethodForHandling { get; } = () => { };

        public GlobalOptionsManager GlobalOptionsManager { get; internal set; } = default!; // Usage of default because this property is set by Starter class and the object is shared among multiple classes, but I don't want to add it to the constructor.
        public GlobalOptionsType? GetGlobalOptions<GlobalOptionsType>() where GlobalOptionsType : class, IGlobalOptionsContainer
        {
            return GlobalOptionsManager.GetGlobalOptions<GlobalOptionsType>();
        }
        public GlobalOptionsType? GO<GlobalOptionsType>() where GlobalOptionsType : class, IGlobalOptionsContainer
        {
            return GetGlobalOptions<GlobalOptionsType>();
        }
        //TODO: Transfer to IStarterCommand what's above

        protected StarterCommand() : base(TEMPORARY_NAME) 
        {
            Name = this.GetType().Name.PascalToKebabCase();
        }

        protected StarterCommand(string name, string? description = null) : base(TEMPORARY_NAME, description)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(nameof(name) + " parameter can't be null or white spaces");
            }

            if (name.Contains('-'))
            {
                Name = name.ToLower();
            }
            else
            {
                Name = name.PascalToKebabCase();
            }
        }


        public string GetFullCommandString()
        {
            var parentsName = new List<string>();
            var currentCommand = (Symbol)this;
            
            while (currentCommand != null)
            {
                parentsName.Add(currentCommand.Name);
                currentCommand = currentCommand.Parents.FirstOrDefault();
            }
            parentsName.Reverse();
            parentsName = parentsName.Skip(1).ToList();

            return String.Join(" ", parentsName);
        }

        internal void Initialize(Command? receptacle = null)
        {
            receptacle ??= this;

            if (this.Subcommands.Count == 0) // Only leaves can execute code
            {
                Handler = CommandHandler.Create(HandleCommand);
                LoadArguments(receptacle);
            }

            LoadOptions(this.GetType(), receptacle);

            Description = GatherDescription(this.GetType());
            IsHidden = Attribute.IsDefined(this.GetType(), typeof(HiddenAttribute));
        }

        private void LoadArguments(Command receptacle)
        {
            var parameters = MethodForHandling.Method.GetParameters();
            foreach (var parameter in parameters)
            {
                if (parameter.Name == null) continue; // Skipping param without name

                var argumentType = typeof(Argument<>).MakeGenericType(parameter.ParameterType);
                var constructor = argumentType.GetConstructor(Type.EmptyTypes);
                var argument = (Argument)constructor!.Invoke(null);
                argument.Name = parameter.Name;
                argument.Description = GatherDescription(parameter);
                argument.IsHidden = Attribute.IsDefined(parameter, typeof(HiddenAttribute));
                if (parameter.DefaultValue is not System.DBNull)
                {
                    argument.SetDefaultValue(parameter.DefaultValue);
                }
                receptacle.Add(argument);
            }
        }

        private int HandleCommand(InvocationContext context)
        {
            // Handle global options
            foreach(var globalOptionsType in GlobalOptionsManager.GlobalOptionsTypes)
            {
                var handleGlobalOptions = this.GetType()
                    .GetMethod(nameof(HandleGlobalOptions), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(globalOptionsType);
                CommandHandler.Create(handleGlobalOptions).Invoke(context);
            }

            // Handle options
            var handleCommandOptionsMethod = this.GetType() //typeof(StarterCommand)
                .GetMethod(nameof(HandleCommandOptions), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(this.GetType());
            CommandHandler.Create(handleCommandOptionsMethod).Invoke(context);

            // Handle command execution
            return CommandHandler.Create(MethodForHandling).Invoke(context); //TODO: Manage async
        }

        /// <summary>
        /// Copy the properties to the command
        /// </summary>
        /// <typeparam name="Self">Type of the command</typeparam>
        /// <param name="context">Parsing context</param>
        /// <param name="self">Filled command provided by System.CommandLine</param>
        /// <remarks>This method has to have "protected" visibility, otherwise it doesn't work</remarks>
        protected void HandleCommandOptions<Self>(InvocationContext context, Self self) where Self : Command
        {
            var currentCommand = context.BindingContext.ParseResult.CommandResult.Command; // Using "this" is not the same object
            if (currentCommand is RootCommand)
            {
                currentCommand = currentCommand.Subcommands[0];
            }
            var selfProperties = GetProperties(self);
            var thisProperties = GetProperties(currentCommand);

            foreach ( var selfProperty in selfProperties )
            {
                var thisProperty = thisProperties.FirstOrDefault(p => p.Equals(selfProperty));
                if (thisProperty == null) continue;

                var value = selfProperty.GetValue(self);
                thisProperty.SetValue(currentCommand, value);
            }
        }
        protected void HandleGlobalOptions<GlobalOptionsType>(InvocationContext context, GlobalOptionsType globalOptions) where GlobalOptionsType : class, IGlobalOptionsContainer
        {
            GlobalOptionsManager.SetGlobalOptions<GlobalOptionsType>(globalOptions);
        }
    }
}
