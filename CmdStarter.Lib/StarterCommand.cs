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
using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;

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

        public virtual Delegate MethodForHandling { get; } = () => { };


        internal void Initialize(Command? receptacle = null)
        {
            receptacle ??= this;

            if (this.Subcommands.Count == 0) // Only leaves can execute code
            {
                Handler = CommandHandler.Create(HandleCommand);
                LoadArguments(receptacle);
            }

            LoadAliases(receptacle, this.GetType().GetCustomAttributes<AliasAttribute>());
            LoadOptions(receptacle);

            Description = GatherDescription(this.GetType());
            IsHidden = Attribute.IsDefined(this.GetType(), typeof(HiddenAttribute));
        }

        private void LoadAliases(IdentifierSymbol receptacle, IEnumerable<AliasAttribute> attributes)
        {
            var aliases = attributes.SelectMany(attribute => attribute.Aliases);
            foreach (var alias in aliases)
            {
                receptacle.AddAlias(alias);
            }
        }

        private void LoadOptions(Command receptacle)
        {
            var properties = GetProperties(this);

            foreach (var property in properties)
            {
                var isList = this.GetType()
                    .GetInterfaces()
                    .Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>));

                var optionType = typeof(Option<>).MakeGenericType(property.PropertyType);
                var constructor = optionType.GetConstructor(new Type[] { typeof(string), typeof(string) });
                var optionName = OPTION_PREFIX + property.Name.PascalToKebabCase();
                var option = (Option)constructor!.Invoke(new object[] { optionName, string.Empty });
                var aliasAttributes = property.GetCustomAttributes<AliasAttribute>();
                var autoCompletions = property.GetCustomAttributes<AutoCompleteAttribute>();

                option.Description = GatherDescription(property);
                option.IsRequired = Attribute.IsDefined(property, typeof(RequiredAttribute));
                option.IsHidden = Attribute.IsDefined(property, typeof(HiddenAttribute));
                option.AllowMultipleArgumentsPerToken = isList;

                LoadAliases(option, aliasAttributes);

                foreach (var completion in autoCompletions)
                {
                    option.AddCompletions(completion.Context);
                }

                receptacle.AddOption(option);
            }
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
                var autoCompletions = parameter.GetCustomAttributes<AutoCompleteAttribute>();

                argument.Name = parameter.Name;
                argument.Description = GatherDescription(parameter);
                argument.IsHidden = Attribute.IsDefined(parameter, typeof(HiddenAttribute));
                if (parameter.DefaultValue is not System.DBNull)
                {
                    argument.SetDefaultValue(parameter.DefaultValue);
                }

                foreach (var completion in autoCompletions)
                {
                    argument.AddCompletions(completion.Context);
                }

                receptacle.Add(argument);
            }
        }

        private static string GatherDescription(ICustomAttributeProvider provider)
        {
            //TODO: Test was not written for this (The command/class usage)
            var descriptions = provider.GetCustomAttributes(false)
                .Where(a => a is DescriptionAttribute)
                .Select(a => ((DescriptionAttribute)a).Description);
            var description = descriptions?.Aggregate(
                    new StringBuilder(), 
                    (current, next) => current.Append(current.Length == 0 ? "" : DESCRIPTION_JOINER).Append(next)
                ).ToString() ?? string.Empty;
            return description;
        }

        private int HandleCommand(InvocationContext context)
        {
            var handleCommandOptionsMethod = this.GetType() //typeof(StarterCommand)
                .GetMethod(nameof(HandleCommandOptions), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(this.GetType());
            CommandHandler.Create(handleCommandOptionsMethod).Invoke(context);
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
    }
}
