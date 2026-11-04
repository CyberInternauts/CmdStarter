using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter.Extensions;
using CyberInternauts.CmdStarter.Interfaces;
using System.CommandLine;
using System.CommandLine.Completions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using static CyberInternauts.CmdStarter.Reflection.Helper;
using CyberInternauts.CmdStarter.Binding;

namespace CyberInternauts.CmdStarter.Reflection;

internal static class Loader
{

    internal static List<OptionBinding> LoadOptions(Type from, Command receptacle)
    {
        var bindings = new List<OptionBinding>();
        var properties = GetProperties(from);

        foreach (var property in properties)
        {
            var isList = property.PropertyType.GetInterfaces()
                .Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>));

            var optionType = typeof(Option<>).MakeGenericType(property.PropertyType);
            var constructor = optionType.GetConstructor(new Type[] { typeof(string), typeof(string[]) });
            var optionName = StarterCommand.OptionPrefix + property.Name.PascalToKebabCase();
            var option = (Option)constructor!.Invoke(new object[] { optionName, Array.Empty<string>() });
            option.Required = Attribute.IsDefined(property, typeof(RequiredAttribute));
            AddValidationAttributes(property, option, optionName.TrimStart('-'));
            option.Hidden = Attribute.IsDefined(property, typeof(HiddenAttribute));
            option.AllowMultipleArgumentsPerToken = isList;
            LoadDescription(property, option);
            LoadAliases(property, option.Aliases);
            LoadAutoCompletes(property, completion => option.CompletionSources.Add(completion));

            if (typeof(IGlobalOptionsContainer).IsAssignableFrom(from))
            {
                option.Recursive = true;
                receptacle.Options.Add(option);
            }
            else
            {
                receptacle.Options.Add(option);
            }
            bindings.Add(new OptionBinding(property, option));
        }
        return bindings;
    }

    internal static Argument?[] LoadArguments(MethodInfo method, Command receptacle)
    {
        var parameters = method.GetParameters();
        var arguments = new Argument?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (parameter.ParameterType == typeof(CancellationToken)) continue;
            if (parameter.Name == null) continue; // Skipping param without name
            var argument = CreateArgument(parameter);
            receptacle.Arguments.Add(argument);
            arguments[i] = argument;
        }
        return arguments;
    }

    internal static Argument CreateArgument(ParameterInfo parameter)
    {
        var argumentType = typeof(Argument<>).MakeGenericType(parameter.ParameterType);
        var constructor = argumentType.GetConstructor(new[] { typeof(string) });
        var argument = (Argument)constructor!.Invoke(new object[] { parameter.Name! });
        argument.Hidden = Attribute.IsDefined(parameter, typeof(HiddenAttribute));
        AddValidationAttributes(parameter, argument, parameter.Name!);
        if (parameter.DefaultValue is not System.DBNull)
        {
            typeof(Loader).GetMethod(nameof(SetDefaultValue), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(parameter.ParameterType).Invoke(null, new[] { argument, parameter.DefaultValue });
        }
        LoadDescription(parameter, argument);
        LoadAutoCompletes(parameter, completion => argument.CompletionSources.Add(completion));
        return argument;
    }

    internal static Option CreateParameterOption(ParameterInfo parameter)
    {
        var optionType = typeof(Option<>).MakeGenericType(parameter.ParameterType);
        var constructor = optionType.GetConstructor(new[] { typeof(string), typeof(string[]) });
        var option = (Option)constructor!.Invoke(new object[] { StarterCommand.OptionPrefix + parameter.Name!.PascalToKebabCase(), Array.Empty<string>() });
        option.Required = Attribute.IsDefined(parameter, typeof(RequiredAttribute));
        AddValidationAttributes(parameter, option, parameter.Name!.PascalToKebabCase());
        option.Hidden = Attribute.IsDefined(parameter, typeof(HiddenAttribute));
        option.AllowMultipleArgumentsPerToken = parameter.ParameterType.GetInterfaces()
            .Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>));
        LoadDescription(parameter, option);
        LoadAliases(parameter, option.Aliases);
        LoadAutoCompletes(parameter, completion => option.CompletionSources.Add(completion));
        if (parameter.HasDefaultValue && parameter.DefaultValue is not DBNull)
        {
            typeof(Loader).GetMethod(nameof(SetDefaultOptionValue), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(parameter.ParameterType).Invoke(null, new[] { option, parameter.DefaultValue });
        }
        return option;
    }

    internal static void LoadDescription(ICustomAttributeProvider provider, Symbol receptacle)
    {
        var descriptions = provider.GetCustomAttributes(false)
            .Where(a => a is DescriptionAttribute)
            .Select(a => ((DescriptionAttribute)a).Description);
        var description = descriptions?.Aggregate(
                new StringBuilder(),
                (current, next) => current.Append(current.Length == 0 ? "" : StarterCommand.DescriptionJoiner).Append(next)
            ).ToString() ?? string.Empty;

        receptacle.Description = description;
    }

    internal static void LoadAliases(ICustomAttributeProvider provider, ICollection<string> aliasesTarget)
    {
        var aliases = provider.GetCustomAttributes(false)
            .Where(a => a is AliasAttribute)
            .Cast<AliasAttribute>()
            .SelectMany(a => a.Aliases);

        foreach (var alias in aliases)
        {
            aliasesTarget.Add(alias);
        }
    }

    internal static void LoadAutoCompletes(ICustomAttributeProvider provider, Action<Func<CompletionContext, IEnumerable<CompletionItem>>> action)
    {
        var completionDelegates = GetAutoCompleteAttributes(provider)
            .Select(attribute => attribute.Context);

        foreach (var completion in completionDelegates)
        {
            action(completion);
        }
    }

    private static void SetDefaultValue<T>(Argument<T> argument, object? value) =>
        argument.DefaultValueFactory = _ => (T)value!;

    private static void SetDefaultOptionValue<T>(Option<T> option, object? value) =>
        option.DefaultValueFactory = _ => (T)value!;

    private static IEnumerable<AutoCompleteAttribute> GetAutoCompleteAttributes(ICustomAttributeProvider provider)
    {
        return provider.GetCustomAttributes(false)
            .Where(attribute => attribute is AutoCompleteAttribute)
            .Cast<AutoCompleteAttribute>();
    }

    private static void AddValidationAttributes(ICustomAttributeProvider provider, Option option, string displayName)
    {
        foreach (var attribute in GetValidationAttributes(provider))
        {
            option.Validators.Add(result =>
            {
                var optionResult = result.GetResult(option);
                if (optionResult is null || optionResult.Implicit)
                {
                    return;
                }

                var value = optionResult.GetValueOrDefault<object?>();
                var context = new ValidationContext(new object())
                {
                    DisplayName = displayName,
                    MemberName = displayName
                };
                var validationResult = attribute.GetValidationResult(value, context);
                if (validationResult != ValidationResult.Success)
                {
                    result.AddError(validationResult!.ErrorMessage!);
                }
            });
        }
    }

    private static void AddValidationAttributes(ICustomAttributeProvider provider, Argument argument, string displayName)
    {
        foreach (var attribute in GetValidationAttributes(provider))
        {
            argument.Validators.Add(result =>
            {
                var argumentResult = result.GetResult(argument);
                if (argumentResult is null || argumentResult.Implicit)
                {
                    return;
                }

                var value = argumentResult.GetValueOrDefault<object?>();
                var context = new ValidationContext(new object())
                {
                    DisplayName = displayName,
                    MemberName = displayName
                };
                var validationResult = attribute.GetValidationResult(value, context);
                if (validationResult != ValidationResult.Success)
                {
                    result.AddError(validationResult!.ErrorMessage!);
                }
            });
        }
    }

    private static IEnumerable<ValidationAttribute> GetValidationAttributes(ICustomAttributeProvider provider) =>
        provider.GetCustomAttributes(false).OfType<ValidationAttribute>();
}
