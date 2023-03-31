using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using com.cyberinternauts.csharp.CmdStarter.Lib.Extensions;
using System.CommandLine;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using static com.cyberinternauts.csharp.CmdStarter.Lib.Reflection.Helper;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Reflection
{
    public static class CommandLineHelper
    {

        public static void LoadOptions(Type from, Command receptacle)
        {
            var properties = GetProperties(from);

            foreach (var property in properties)
            {
                //TODO: There was an error here... this means Option.AllowMultipleArgumentsPerToken is not tested.
                var isList = property.PropertyType.GetInterfaces()
                    .Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>));

                var optionType = typeof(Option<>).MakeGenericType(property.PropertyType);
                var constructor = optionType.GetConstructor(new Type[] { typeof(string), typeof(string) });
                var optionName = StarterCommand.OPTION_PREFIX + property.Name.PascalToKebabCase();
                var option = (Option)constructor!.Invoke(new object[] { optionName, string.Empty });
                option.Description = GatherDescription(property);
                option.IsRequired = Attribute.IsDefined(property, typeof(RequiredAttribute));
                option.IsHidden = Attribute.IsDefined(property, typeof(HiddenAttribute));
                option.AllowMultipleArgumentsPerToken = isList;
                receptacle.AddOption(option);
            }
        }

        public static string GatherDescription(ICustomAttributeProvider provider)
        {
            var descriptions = provider.GetCustomAttributes(false)
                .Where(a => a is DescriptionAttribute)
                .Select(a => ((DescriptionAttribute)a).Description);
            var description = descriptions?.Aggregate(
                    new StringBuilder(),
                    (current, next) => current.Append(current.Length == 0 ? "" : StarterCommand.DESCRIPTION_JOINER).Append(next)
                ).ToString() ?? string.Empty;
            return description;
        }
    }
}
