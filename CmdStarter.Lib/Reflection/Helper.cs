using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using System.Reflection;
using System.Text.RegularExpressions;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Reflection
{
    public static class Helper
    {

        private static List<string>? interfacePropertiesNames = null;

        public static MethodInfo FindGetInstanceMethod(Type commandType)
        {
            const string methodName = nameof(IStarterCommand.GetInstance);

            // Look through classes hierarchy from top to bottom
            MethodInfo? methodInfo = null;
            var currentType = commandType;
            while (
                methodInfo == null 
                && currentType != null 
                && currentType.IsAssignableTo(typeof(IStarterCommand))
                && !currentType.Equals(typeof(StarterCommand)) // Ensures not looping back
                )
            {
                methodInfo = currentType.GetMethod(methodName);
                currentType = currentType.BaseType;
            }
            // Use default interface method implementation
            if (methodInfo == null)
            {
                methodInfo = typeof(IStarterCommand).GetMethod(methodName)!;
            }
            var method = methodInfo.MakeGenericMethod(commandType);
            return method;
        }

        public static IEnumerable<PropertyInfo> GetProperties(object obj)
        {
            return GetProperties(obj.GetType());
        }

        public static IEnumerable<PropertyInfo> GetProperties(Type type)
        {
            LoadInterfaceProperties();

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p =>
                    p.CanWrite && p.CanRead
                    && 
                    (
                      ((p.DeclaringType?.IsAssignableTo(typeof(IStarterCommand)) ?? false)
                      && !interfacePropertiesNames!.Contains(p.Name)) // Can't be null because already loaded
                    ||
                    (p.DeclaringType?.IsAssignableTo(typeof(IGlobalOptionsContainer)) ?? false))
                );

            return properties;
        }

        public static IEnumerable<Type> FilterTypesByNamespaces(IEnumerable<Type> commandsTypes, List<string> namespaces)
        {
            var nbCommands = commandsTypes.Count();
            if (nbCommands == 0 || !namespaces.Any()) return commandsTypes;

            var namespacesIncluded = namespaces.Where(n => !String.IsNullOrWhiteSpace(n) && !n.StartsWith(Starter.EXCLUSION_SYMBOL));
            var hasIncluded = namespacesIncluded.Any();
            var namespacesExcluded = namespaces.Where(n => !String.IsNullOrWhiteSpace(n) && n.StartsWith(Starter.EXCLUSION_SYMBOL));

            commandsTypes = commandsTypes.Where(c =>
            {
                var outNamespaces = namespacesExcluded.Any(n => c.Namespace?.StartsWith(n[1..]) ?? false);
                var inNamespaces = !hasIncluded || namespacesIncluded.Any(n => c.Namespace?.StartsWith(n) ?? false);

                return !outNamespaces && inNamespaces;
            });

            return commandsTypes;
        }

        public static IEnumerable<Type> FilterTypesByClasses(IEnumerable<Type> commandsTypes, List<string> classes)
        {
            var nbCommands = commandsTypes.Count();
            if (nbCommands == 0 || !classes.Any()) return commandsTypes;

            bool onlyExclude = classes.All(filter => filter.StartsWith(Starter.EXCLUSION_SYMBOL));

            Regex dotRegex = new(@"\\.");

            Regex[] excludes = classes.Where(filter => filter.StartsWith(Starter.EXCLUSION_SYMBOL))
                .Select(filter =>
                {
                    var pattern = WildcardsToRegex(filter[1..]);
                    return new Regex(pattern, RegexOptions.RightToLeft);
                }).ToArray();

            Regex[] filters = classes.Where(filter => !filter.StartsWith(Starter.EXCLUSION_SYMBOL))
                .Select(filter =>
                {
                    var pattern = WildcardsToRegex(filter);
                    return new Regex(pattern, RegexOptions.RightToLeft);
                }).ToArray();

            commandsTypes = commandsTypes.Where(type =>
            {
                bool included = (onlyExclude || filters.Any(rgx => rgx.IsMatch(type.FullName ?? string.Empty)));

                bool excluded = excludes.Any(rgx => rgx.IsMatch(type.FullName ?? string.Empty));

                return included && !excluded;
            });

            return commandsTypes;
        }

        private static string WildcardsToRegex(string wildcard)
        {
            const string STAR_PLACEHOLDER = "<-starplaceholder->";

            return (@$"(.|^){wildcard}$")
                .Replace(".", @"\.")
                .Replace(Starter.ANY_CHAR_SYMBOL_INCLUDE_DOTS, ".")
                .Replace(Starter.ANY_CHAR_SYMBOL, @"\w")
                .Replace(Starter.MULTI_ANY_CHAR_SYMBOL_INCLUDE_DOTS, @$".{STAR_PLACEHOLDER}")
                .Replace(Starter.MULTI_ANY_CHAR_SYMBOL, @"\w*")
                .Replace(STAR_PLACEHOLDER, "*");
        }

        private static void LoadInterfaceProperties()
        {
            if (interfacePropertiesNames != null) return;

            interfacePropertiesNames = typeof(IStarterCommand).GetProperties().Select(p => p.Name).ToList();
        }
    }
}
