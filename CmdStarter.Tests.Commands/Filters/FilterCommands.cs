using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Tests.Commands.Filters;

public sealed class FilterLog
{
    public static FilterLog Current { get; set; } = new();

    public List<string> Entries { get; } = new();
}

public sealed class Probe : StarterCommand
{
    public int Value { get; set; }

    public override Delegate HandlingMethod => (Func<int>)(() =>
    {
        FilterLog.Current.Entries.Add("handler");
        return Value;
    });
}

public static class ProbeMethods
{
    [Command("probe-method")]
    public static int Run() => 21;
}

[AvailableInState("open")]
public sealed class Locked : StarterCommand
{
    public override Delegate HandlingMethod => () => 0;
}

public sealed class AroundFilter(FilterLog log, string name) : ICommandFilter
{
    public Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next)
    {
        log.Entries.Add($"{name}-before");
        return InvokeNextAsync(context, next);
    }

    private async Task<int> InvokeNextAsync(CommandContext context, Func<Task<int>> next)
    {
        var code = await next();
        log.Entries.Add($"{name}-after");
        return code;
    }
}

public sealed class ShortCircuitFilter(int exitCode) : ICommandFilter
{
    public Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next) => Task.FromResult(exitCode);
}

public sealed class IncrementFilter : ICommandFilter
{
    public async Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next) => await next() + 1;
}

public sealed class ThrowingFilter : ICommandFilter
{
    public Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next) => throw new InvalidOperationException("filter failed");
}

public sealed class DisposableFilter : ICommandFilter, IDisposable
{
    public static int Disposals { get; set; }

    public Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next) => next();

    public void Dispose() => Disposals++;
}

public sealed class AsyncDisposableFilter : ICommandFilter, IAsyncDisposable
{
    public static int AsyncDisposals { get; set; }

    public Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next) => next();

    public ValueTask DisposeAsync()
    {
        AsyncDisposals++;
        return ValueTask.CompletedTask;
    }
}

public sealed class FilterScopedService
{
}

public sealed class ScopedFilter(FilterScopedService service) : ICommandFilter
{
    public static List<FilterScopedService> ResolvedServices { get; } = new();

    public Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next)
    {
        ResolvedServices.Add(service);
        return next();
    }
}
