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
    /// <summary>
    /// Abstract class that is the base of all. It transposes what's needed from the <see cref="IStarterCommand"/> object to System.CommandLine commands features.
    /// </summary>
    [AllOptionsExcluded]
    public abstract class StarterCommand : Command, IStarterCommand
    {
        /// <summary>
        /// Options prefix
        /// </summary>
        public const string OPTION_PREFIX = "--";

        /// <summary>
        /// Description joiner upon multiple attribute
        /// </summary>
        public const string DESCRIPTION_JOINER = "\n";

        private const string TEMPORARY_NAME = "temp";

        /// <summary>
        /// <see cref="IStarterCommand"/> object that manages execution and from where the command features are filled
        /// </summary>
        public IStarterCommand UnderlyingCommand { get; internal set; }

        /// <inheritdoc cref="IStarterCommand.HandlingMethod"/>
        public virtual Delegate HandlingMethod { get; } = IStarterCommand.EMPTY_EXECUTION;

        /// <inheritdoc cref="IStarterCommand.GlobalOptionsManager"/>
        public virtual GlobalOptionsManager? GlobalOptionsManager { get; set; }

        /// <summary>
        /// Constructor using its own type as command name
        /// </summary>
        protected StarterCommand() : base(TEMPORARY_NAME) 
        {
            UnderlyingCommand = this;
            Name = this.GetType().Name.PascalToKebabCase();
        }

        /// <summary>
        /// Constructor with name and potentially description
        /// </summary>
        /// <param name="name">Name of the command</param>
        /// <param name="description">Description of the command</param>
        /// <exception cref="ArgumentException">Name can't be null, empty or only whitespace</exception>
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

        /// <summary>
        /// Get the full command path: this command name preceeded by all its parents names.
        /// </summary>
        /// <returns></returns>
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

            var commandType = UnderlyingCommand.GetType();
            LoadOptions(commandType, receptacle);
            LoadDescription(commandType, receptacle);
            LoadAliases(commandType, receptacle);

            IsHidden = Attribute.IsDefined(UnderlyingCommand.GetType(), typeof(HiddenAttribute));
        }

        private async Task HandleGlobalOptions(InvocationContext context)
        {
            if (GlobalOptionsManager == null) return;

            foreach (var globalOptionsType in GlobalOptionsManager.GlobalOptionsTypes)
            {
                var handleGlobalOptions = GlobalOptionsManager.GetType()
                    .GetMethod(nameof(GlobalOptionsManager.SetGlobalOptions), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(globalOptionsType);
                await CommandHandler.Create(handleGlobalOptions, GlobalOptionsManager).InvokeAsync(context);
            }
        }

        private async Task<int> HandleCommand(InvocationContext context)
        {
            // Handle global options
            await HandleGlobalOptions(context);

            // Handle options
            var handleCommandOptionsMethod = typeof(HandlerStarterCommand) // Using <see cref="HandlerStarterCommand"> because it is not abstract nor generic
                .GetMethod(nameof(HandleCommandOptions), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(UnderlyingCommand.GetType());
            await CommandHandler.Create(handleCommandOptionsMethod).InvokeAsync(context);

            // Handle command execution
            return await CommandHandler.Create(HandlingMethod).InvokeAsync(context);
        }

        /// <summary>
        /// Copy the parsed properties to a receptable
        /// </summary>
        /// <typeparam name="ParsingType">Type of the object to fill from parsing</typeparam>
        /// <param name="context">Parsing context</param>
        /// <param name="parsed">Filled object provided by System.CommandLine</param>
        /// <remarks>
        /// - This method has to have "internal" visibility not "private", otherwise it doesn't work because it uses a derived class as method container
        /// - This method cannot be static
        /// </remarks>
#pragma warning disable CA1822 // Mark members as static
        internal void HandleCommandOptions<ParsingType>(InvocationContext context, ParsingType parsed) where ParsingType : IStarterCommand
#pragma warning restore CA1822 // Mark members as static
        {
            var currentCommand = context.BindingContext.ParseResult.CommandResult.Command; // Using "this" is not the same object
            if (currentCommand is RootCommand)
            {
                currentCommand = currentCommand.Subcommands[0];
            }
            var starterCommand = currentCommand as StarterCommand;
            var receptable = starterCommand?.UnderlyingCommand ?? (object)currentCommand;
            var parsedProperties = GetProperties(parsed);
            var receptacleProperties = GetProperties(receptable);

            foreach ( var parsedProperty in parsedProperties )
            {
                var receptacleProperty = receptacleProperties.FirstOrDefault(p => p.Equals(parsedProperty));
                if (receptacleProperty == null) continue;

                var value = parsedProperty.GetValue(parsed);
                receptacleProperty.SetValue(receptable, value);
            }
        }

        /// <summary>
        /// Obtain an instance of a derived <see cref="StarterCommand"/>:<br/>
        /// <list type="bullet">
        /// <item>The type itself if it derives already from <see cref="StarterCommand"/></item>
        /// <item>A <see cref="GenericStarterCommand{CommandType}"/> using <typeparamref name="CommandType"/></item>
        /// </list>
        /// </summary>
        /// <typeparam name="CommandType">Command type to obtain a instance of</typeparam>
        /// <returns>A derived instance of <see cref="StarterCommand"/></returns>
        public static StarterCommand GetInstance<CommandType>() where CommandType : IStarterCommand
        {
            var commandType = typeof(CommandType);

            if (commandType.IsAssignableTo(typeof(StarterCommand)))
            {
                var method = FindGetInstanceMethod(commandType);
                return (StarterCommand)method.Invoke(null, null)!;
            }

            return new GenericStarterCommand<CommandType>();
        }
    }
}
