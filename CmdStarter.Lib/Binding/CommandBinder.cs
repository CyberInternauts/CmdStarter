using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Binding;

internal static class CommandBinder
{
    internal static bool TryGetOptionValue(ParseResult parseResult, Option option, out object? value)
    {
        if (parseResult.FindResultFor(option) is null)
        {
            value = null;
            return false;
        }

        value = parseResult.GetValueForOption(option);
        return true;
    }

    internal static object? GetArgumentValue(ParseResult parseResult, Argument argument) =>
        parseResult.GetValueForArgument(argument);

    internal static void BindOptions(ParseResult parseResult, object target, IEnumerable<OptionBinding> bindings)
    {
        foreach (var binding in bindings)
        {
            var value = TryGetOptionValue(parseResult, binding.Option, out var parsedValue)
                ? parsedValue
                : binding.InitialValue;
            binding.Property.SetValue(target, value);
        }
    }

    internal static object CreateGlobalOptions(ParseResult parseResult, Type type, IEnumerable<OptionBinding> bindings)
    {
        var instance = Activator.CreateInstance(type)!;
        foreach (var binding in bindings)
        {
            if (TryGetOptionValue(parseResult, binding.Option, out var value))
            {
                binding.Property.SetValue(instance, value);
            }
        }
        return instance;
    }

    internal static async Task<int> InvokeHandlerAsync(ParseResult parseResult, Delegate handler, Argument?[] arguments)
    {
        var parameters = handler.Method.GetParameters();
        var values = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var argument = i < arguments.Length ? arguments[i] : null;
            if (argument is not null)
            {
                values[i] = GetArgumentValue(parseResult, argument);
            }
            else if (parameters[i].HasDefaultValue)
            {
                values[i] = parameters[i].DefaultValue;
            }
            else
            {
                var parameterType = parameters[i].ParameterType;
                values[i] = parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null;
            }
        }

        object? result;
        try
        {
            result = handler.DynamicInvoke(values);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }

        if (result is Task<int> intTask) return await intTask;
        if (result is Task task)
        {
            await task;
            return 0;
        }
        return result is int exitCode ? exitCode : 0;
    }
}
