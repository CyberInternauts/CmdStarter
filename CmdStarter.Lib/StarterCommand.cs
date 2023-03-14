using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Runtime.CompilerServices;
using System.ComponentModel;

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

        protected StarterCommand() : base(TEMPORARY_NAME) 
        {
            Name = ConvertToKebabCase(this.GetType().Name);
            Initialize();
        }

        protected StarterCommand(string name, string? description = null)
            : base(ConvertToKebabCase(name), description)
        {
            Initialize();
        }

        private void Initialize()
        {
            //TODO: Do real command handler (Already bad, because when a command is not a leaf, it shall show help
            //Handler = CommandHandler.Create(() => Console.WriteLine("Shall do " + this.GetType().Name));

            //TODO: Test was not written for this
            var descriptions = this.GetType().GetCustomAttributes(false).Where(a => a is DescriptionAttribute)
                .Select(a => (a as DescriptionAttribute)!.Description);
            var description = descriptions?.Aggregate(
                    new StringBuilder(), (current, next) => current.Append(current.Length == 0 ? "" : ", ").Append(next)
                ).ToString();
            Description = descriptions?.Any() ?? false ? description : "Fake";
        }

        private static string ConvertToKebabCase(string input)
        {
            var output = string.Empty;
            for (var i = 0; i < input.Length; i++)
            {
                var c = input[i];
                if (Char.IsUpper(c) && output != string.Empty) output += "-";
                output += c;
            }
            output = output.ToLower();
            return output;
        }
    }
}
