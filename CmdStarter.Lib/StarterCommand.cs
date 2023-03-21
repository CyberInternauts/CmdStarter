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
        private const string TEMPORARY_NAME = "temp";

        public virtual Delegate MethodForHandling { get; } = () => { };

        protected StarterCommand() : base(TEMPORARY_NAME) 
        {
            Name = this.GetType().Name.PascalToKebabCase()!;
        }

        protected StarterCommand(string name, string? description = null)
            : base(name.PascalToKebabCase() ?? string.Empty, description)
        {
        }

        internal void Initialize()
        {
            if (this.Subcommands.Count == 0) // Only leaves can execute code
            {
                Handler = CommandHandler.Create(HandleCommand);
                LoadArguments();
            }

            LoadDescription();
        }

        private void LoadArguments()
        {
            var parameters = MethodForHandling.Method.GetParameters();
            foreach (var parameter in parameters)
            {
                if (parameter.Name == null) continue; // Skipping param without name

                var description = (parameter.GetCustomAttributes(false)
                        .FirstOrDefault(a => a is System.ComponentModel.DescriptionAttribute) as System.ComponentModel.DescriptionAttribute)
                        ?.Description;

                var argumentType = typeof(Argument<>).MakeGenericType(parameter.ParameterType);
                var constructor = argumentType.GetConstructor(Type.EmptyTypes);
                var argument = (Argument)constructor!.Invoke(null);
                argument.Name = parameter.Name;
                argument.Description = description;
                if (parameter.DefaultValue is not System.DBNull)
                {
                    argument.SetDefaultValue(parameter.DefaultValue);
                }
                this.Add(argument);
            }
        }

        private void LoadDescription()
        {
            //TODO: Test was not written for this
            var descriptions = this.GetType().GetCustomAttributes(false).Where(a => a is DescriptionAttribute)
                .Select(a => (a as DescriptionAttribute)!.Description);
            var description = descriptions?.Aggregate(
                    new StringBuilder(), (current, next) => current.Append(current.Length == 0 ? "" : ", ").Append(next)
                ).ToString();
            Description = descriptions?.Any() ?? false ? description : "Fake";
        }

        private int HandleCommand(InvocationContext context)
        {
            //TODO: When doing options, enable this: CommandHandler.Create(HandleCommandOptions).Invoke(context);
            return CommandHandler.Create(MethodForHandling).Invoke(context); //TODO: Manage async
        }
    }
}
