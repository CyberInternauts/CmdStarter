using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Collections.Concurrent;

namespace CyberInternauts.CmdStarter.Binding;

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

    // symbolType is Option<> or Argument<>: the lookup is by exact signature, whatever the order of the methods of ParseResult.
    private static MethodInfo FindGetter(Type symbolType) => typeof(ParseResult)
        .GetMethod("GetValue", 1, new[] { symbolType.MakeGenericType(Type.MakeGenericMethodParameter(0)) })
        ?? throw new InvalidOperationException($"{typeof(ParseResult).FullName}.GetValue<T>({symbolType.Name}) was not found: this version of System.CommandLine is not supported.");

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

    internal static async Task<int> InvokeHandlerAsync(ParseResult parseResult, Delegate handler, Argument?[] arguments, CommandConsole console, CancellationToken cancellationToken)
    {
        var parameters = handler.Method.GetParameters();
        var values = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var argument = arguments[i];
            if (argument is not null)
            {
                values[i] = GetArgumentValue(parseResult, argument);
            }
            else if (parameters[i].ParameterType == typeof(CancellationToken))
            {
                values[i] = cancellationToken;
            }
            else if (parameters[i].ParameterType == typeof(CommandConsole))
            {
                values[i] = console;
            }
            else
            {
                var parameterType = parameters[i].ParameterType;
                values[i] = parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null;
            }
        }

        var result = Invoke(() => handler.DynamicInvoke(values));
        return await ToExitCodeAsync(result);
    }

    internal static object? Invoke(Func<object?> call)
    {
        try { return call(); }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    internal static async Task<int> ToExitCodeAsync(object? result)
    {
        if (result is ValueTask<int> valueTaskInt) return await valueTaskInt;
        if (result is ValueTask valueTask) { await valueTask; return 0; }
        if (result is Task<int> intTask) return await intTask;
        if (result is Task task)
        {
            await task;
            return 0;
        }
        return result is int exitCode ? exitCode : 0;
    }
}
