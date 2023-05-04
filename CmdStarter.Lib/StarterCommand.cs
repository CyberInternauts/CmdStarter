using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Invocation;
using com.cyberinternauts.csharp.CmdStarter.Lib.Extensions;
using System.Reflection;
using static com.cyberinternauts.csharp.CmdStarter.Lib.Reflection.Helper;
using static com.cyberinternauts.csharp.CmdStarter.Lib.Reflection.Loader;
using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using com.cyberinternauts.csharp.CmdStarter.Lib.SpecialCommands;

namespace com.cyberinternauts.csharp.CmdStarter.Lib
{ 
    public abstract class StarterCommand : Command, IStarterCommand
    {
        public const string OPTION_PREFIX = "--";
        public const string DESCRIPTION_JOINER = "\n";

        private const string TEMPORARY_NAME = "temp";

        internal IStarterCommand UnderlyingCommand { get; set; }
        
        public virtual Delegate HandlingMethod { get; } = () => { };

        public virtual GlobalOptionsManager? GlobalOptionsManager { get; set; }

        protected StarterCommand() : base(TEMPORARY_NAME) 
        {
            UnderlyingCommand = this;
            Name = this.GetType().Name.PascalToKebabCase();
        }

        protected StarterCommand(string name, string? description = null) : base(TEMPORARY_NAME, description)
        {
            UnderlyingCommand = this;

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

        /// <summary>
        /// Shortcut method for <see cref="IStarterCommandExtensions.GetGlobalOptions{GlobalOptionsType}(IStarterCommand)"/> that doesn't need usage of "this"
        /// </summary>
        /// <typeparam name="GlobalOptionsType"></typeparam>
        /// <returns></returns>
        public GlobalOptionsType? GetGlobalOptions<GlobalOptionsType>() where GlobalOptionsType : class, IGlobalOptionsContainer
        {
            return ((IStarterCommand)this).GetGlobalOptions<GlobalOptionsType>();
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
                LoadArguments(HandlingMethod.Method, receptacle);
            }

            var commandType = this.GetType();
            LoadOptions(commandType, receptacle);
            LoadDescription(commandType, receptacle);
            LoadAliases(commandType, receptacle);

            IsHidden = Attribute.IsDefined(this.GetType(), typeof(HiddenAttribute));
        }

        private void HandleGlobalOptions(InvocationContext context)
        {
            if (GlobalOptionsManager == null) return;

            foreach (var globalOptionsType in GlobalOptionsManager.GlobalOptionsTypes)
            {
                var handleGlobalOptions = GlobalOptionsManager.GetType()
                    .GetMethod(nameof(GlobalOptionsManager.SetGlobalOptions), BindingFlags.Public | BindingFlags.Instance)!
                    .MakeGenericMethod(globalOptionsType);
                CommandHandler.Create(handleGlobalOptions, GlobalOptionsManager).Invoke(context);
            }
        }

        private int HandleCommand(InvocationContext context)
        {
            // Handle global options
            HandleGlobalOptions(context);

            // Handle options
            var handleCommandOptionsMethod = this.GetType() //typeof(StarterCommand)
                .GetMethod(nameof(HandleCommandOptions), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(this.GetType());
            CommandHandler.Create(handleCommandOptionsMethod).Invoke(context);

            // Handle command execution
            return CommandHandler.Create(HandlingMethod).Invoke(context); //TODO: CMD-44 Manage async
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

        public static StarterCommand GetInstance<CommandType>() where CommandType : IStarterCommand
        {
            if (typeof(CommandType).IsAssignableTo(typeof(StarterCommand)))
            {
                return (Activator.CreateInstance(typeof(CommandType)) as StarterCommand)!; // Can't be null because already an IStarterCommand
            }

            return new GenericStarterCommand<CommandType>();
        }

        static IStarterCommand IStarterCommand.GetInstance<CommandType>()
        {
            return StarterCommand.GetInstance<StarterCommand>();
        }
    }
}
