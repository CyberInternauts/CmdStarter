using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.CommandLine.Invocation;
using com.cyberinternauts.csharp.CmdStarter.Lib.Extensions;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

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

        public virtual Delegate MethodForHandling { get; } = () => { };

        internal void Initialize()
        {
            if (this.Subcommands.Count == 0) // Only leaves can execute code
            {
                Handler = CommandHandler.Create(HandleCommand);
                LoadArguments();
            }

            LoadOptions();

            Description = GatherDescription(this.GetType());
        }

        private void LoadOptions()
        {
            var properties = GetProperties(this);

            foreach (var property in properties)
            {
                var isList = this.GetType().GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>));

                var optionType = typeof(Option<>).MakeGenericType(property.PropertyType);
                var constructor = optionType.GetConstructor(new Type[] { typeof(string), typeof(string) });
                var optionName = OPTION_PREFIX + property.Name.PascalToKebabCase();
                var option = (Option)constructor!.Invoke(new object[] { optionName, string.Empty });
                option.Description = GatherDescription(property);
                option.IsRequired = Attribute.IsDefined(property, typeof(RequiredAttribute));
                option.AllowMultipleArgumentsPerToken = isList;
                this.AddOption(option);
            }
        }

        private void LoadArguments()
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
                if (parameter.DefaultValue is not System.DBNull)
                {
                    argument.SetDefaultValue(parameter.DefaultValue);
                }
                this.Add(argument);
            }
        }

        private static string GatherDescription(ICustomAttributeProvider provider)
        {
            //TODO: Test was not written for this (The command/class usage)
            var descriptions = provider.GetCustomAttributes(false).Where(a => a is DescriptionAttribute)
                .Select(a => (a as DescriptionAttribute)!.Description);
            var description = descriptions?.Aggregate(
                    new StringBuilder(), (current, next) => current.Append(current.Length == 0 ? "" : ", ").Append(next)
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

        private static IEnumerable<PropertyInfo> GetProperties(object obj)
        {
            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p =>
                    p.CanWrite && p.CanRead
                    && (p.DeclaringType?.IsSubclassOf(typeof(StarterCommand)) ?? false)
                );

            return properties;
        }
    }
}
