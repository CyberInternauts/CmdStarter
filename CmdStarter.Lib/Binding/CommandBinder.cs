using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Runtime.ExceptionServices;
using CyberInternauts.CmdStarter.Catalog;

namespace CyberInternauts.CmdStarter.Binding;

internal static class CommandBinder
{
    internal static bool TryGetOptionValue(ParseResult parseResult, Option option, out object? value)
    {
        if (parseResult.GetResult(option) is null)
        {
            value = null;
            return false;
        }

        value = SymbolFactories.For(option.ValueType).GetValue(parseResult, option);
        return true;
    }

    internal static object? GetArgumentValue(ParseResult parseResult, Argument argument) =>
        SymbolFactories.For(argument.ValueType).GetValue(parseResult, argument);

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
                values[i] = parameterType.IsValueType ? SymbolFactories.For(parameterType).DefaultValue : null;
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
