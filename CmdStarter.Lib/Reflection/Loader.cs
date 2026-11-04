using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter.Catalog;
using CyberInternauts.CmdStarter.Extensions;
using CyberInternauts.CmdStarter.Interfaces;
using System.CommandLine;
using System.CommandLine.Completions;
using System.CommandLine.Parsing;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using static CyberInternauts.CmdStarter.Reflection.Helper;
using CyberInternauts.CmdStarter.Binding;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace CyberInternauts.CmdStarter.Reflection;

internal static class Loader
{

    // The type of an option is the type of a property or of a parameter of a command that is kept with all its members. A collection type that an option uses is a type that the application uses,
    // and the trimmer keeps the interfaces of a type that is used.
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "The type of an option comes from a property or a parameter of a command that the command catalog keeps with all its members, and the interfaces of a type that the application uses are kept with it.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The type of an option comes from a property or a parameter of a command that the command catalog keeps with all its members, and the interfaces of a type that the application uses are kept with it.")]
    private static bool ImplementsIList(Type type) => type.GetInterfaces()
        .Any(implemented => implemented.IsGenericType && implemented.GetGenericTypeDefinition() == typeof(IList<>));

    internal static List<OptionBinding> LoadOptions([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type from, Command receptacle)
    {
        var bindings = new List<OptionBinding>();
        var properties = GetProperties(from);

        foreach (var property in properties)
        {
            var isList = ImplementsIList(property.PropertyType);

            var optionName = StarterCommand.OptionPrefix + property.Name.PascalToKebabCase();
            var option = SymbolFactories.For(property.PropertyType).CreateOption(optionName);
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
            }
            receptacle.Options.Add(option);
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
            if (parameter.ParameterType == typeof(CancellationToken) || parameter.ParameterType == typeof(CommandConsole)) continue;
            if (parameter.Name == null) continue; // Skipping param without name
            var argument = CreateArgument(parameter);
            receptacle.Arguments.Add(argument);
            arguments[i] = argument;
        }
        return arguments;
    }

    internal static Argument CreateArgument(ParameterInfo parameter)
    {
        var factory = SymbolFactories.For(parameter.ParameterType);
        var argument = factory.CreateArgument(parameter.Name!);
        argument.Hidden = Attribute.IsDefined(parameter, typeof(HiddenAttribute));
        AddValidationAttributes(parameter, argument, parameter.Name!);
        if (parameter.HasDefaultValue)
        {
            factory.SetDefault(argument, parameter.DefaultValue);
        }
        LoadDescription(parameter, argument);
        LoadAutoCompletes(parameter, completion => argument.CompletionSources.Add(completion));
        return argument;
    }

    internal static Option CreateParameterOption(ParameterInfo parameter)
    {
        var factory = SymbolFactories.For(parameter.ParameterType);
        var option = factory.CreateOption(StarterCommand.OptionPrefix + parameter.Name!.PascalToKebabCase());
        option.Required = Attribute.IsDefined(parameter, typeof(RequiredAttribute));
        AddValidationAttributes(parameter, option, parameter.Name!.PascalToKebabCase());
        option.Hidden = Attribute.IsDefined(parameter, typeof(HiddenAttribute));
        option.AllowMultipleArgumentsPerToken = ImplementsIList(parameter.ParameterType);
        LoadDescription(parameter, option);
        LoadAliases(parameter, option.Aliases);
        LoadAutoCompletes(parameter, completion => option.CompletionSources.Add(completion));
        if (parameter.HasDefaultValue && parameter.DefaultValue is not DBNull)
        {
            factory.SetDefault(option, parameter.DefaultValue);
        }
        return option;
    }

    internal static void SetImplicitDefault(Option option, object? value)
    {
        if (option.Required || value is null)
        {
            return;
        }

        if (value is string text && text.Length == 0)
        {
            return;
        }

        if (value is IEnumerable values && value is not string && !values.Cast<object?>().Any())
        {
            return;
        }

        var valueType = option.ValueType;
        var factory = SymbolFactories.For(valueType);
        if (valueType.IsValueType
            && Nullable.GetUnderlyingType(valueType) is null
            && Equals(value, factory.DefaultValue))
        {
            return;
        }

        factory.SetDefault(option, value);
    }

    internal static void LoadDescription(ICustomAttributeProvider provider, Symbol receptacle)
    {
        var descriptions = provider.GetCustomAttributes(false)
            .Where(a => a is DescriptionAttribute)
            .Select(a => ((DescriptionAttribute)a).Description);
        var description = descriptions.Aggregate(
                new StringBuilder(),
                (current, next) => current.Append(current.Length == 0 ? "" : StarterCommand.DescriptionJoiner).Append(next)
            ).ToString();

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
                if (result.Implicit)
                {
                    return;
                }

                Validate(result, attribute, displayName, result.GetValueOrDefault<object?>);
            });
        }
    }

    private static void AddValidationAttributes(ICustomAttributeProvider provider, Argument argument, string displayName)
    {
        foreach (var attribute in GetValidationAttributes(provider))
        {
            argument.Validators.Add(result =>
            {
                // Absence is read from the tokens: System.CommandLine marks an argument result implicit only once its default value is materialized.
                if (result.Tokens.Count == 0)
                {
                    return;
                }

                Validate(result, attribute, displayName, result.GetValueOrDefault<object?>);
            });
        }
    }

    // A value that cannot be converted is already a parse error reported by System.CommandLine,
    // whose GetValueOrDefault throws for it: such a value is left to that error and not validated.
    private static void Validate(SymbolResult result, ValidationAttribute attribute, string displayName, Func<object?> getValue)
    {
        object? value;
        try
        {
            value = getValue();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        // The constructor that takes a display name is the one that does not look for attributes on the type of the instance.
        var context = new ValidationContext(new object(), displayName, serviceProvider: null, items: null)
        {
            MemberName = displayName
        };
        var validationResult = attribute.GetValidationResult(value, context);
        if (validationResult != ValidationResult.Success)
        {
            result.AddError(validationResult!.ErrorMessage ?? $"The value of '{displayName}' is invalid.");
        }
    }

    private static IEnumerable<ValidationAttribute> GetValidationAttributes(ICustomAttributeProvider provider) =>
        provider.GetCustomAttributes(false).OfType<ValidationAttribute>();
}
