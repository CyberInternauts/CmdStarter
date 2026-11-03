using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Collections.Concurrent;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Binding;

internal static class CommandBinder
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> optionGetters = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> argumentGetters = new();

    internal static bool TryGetOptionValue(ParseResult parseResult, Option option, out object? value)
    {
        if (parseResult.GetResult(option) is null)
        {
            value = null;
            return false;
        }

        var getter = optionGetters.GetOrAdd(option.ValueType, valueType => FindGetter(typeof(Option<>)).MakeGenericMethod(valueType));
        value = getter.Invoke(parseResult, new object[] { option });
        return true;
    }

    internal static object? GetArgumentValue(ParseResult parseResult, Argument argument)
    {
        var getter = argumentGetters.GetOrAdd(argument.ValueType, valueType => FindGetter(typeof(Argument<>)).MakeGenericMethod(valueType));
        return getter.Invoke(parseResult, new object[] { argument });
    }

    private static MethodInfo FindGetter(Type symbolType) => typeof(ParseResult)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Where(method => method.Name == "GetValue" && method.IsGenericMethodDefinition)
        .First(method => method.GetParameters() is [{ ParameterType.IsGenericType: true } parameter]
            && parameter.ParameterType.GetGenericTypeDefinition() == symbolType);

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
