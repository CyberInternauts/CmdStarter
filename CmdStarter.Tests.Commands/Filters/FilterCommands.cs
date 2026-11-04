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

public interface IMarker
{
    List<string> Marks { get; }
}

public sealed class Marker : IMarker
{
    public List<string> Marks { get; } = new();
}

public sealed class MarkingFilter(IMarker marker) : ICommandFilter
{
    public Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next)
    {
        marker.Marks.Add("filter: marked");
        return next();
    }
}

public sealed class ServicesProbeFilter : ICommandFilter
{
    public static bool? ServicesWereNull { get; set; }

    public Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next)
    {
        ServicesWereNull = context.Services is null;
        return next();
    }
}

public sealed class ScopedDisposable : IDisposable
{
    public static int Disposals { get; set; }

    public void Dispose() => Disposals++;
}

public sealed class ScopedResolvingFilter(ScopedDisposable resource) : ICommandFilter
{
    public static List<ScopedDisposable> Resolved { get; } = new();

    public Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next)
    {
        Resolved.Add(resource);
        return next();
    }
}

// A command that declares its own static GetInstance, so Starter.Activate takes the GetInstance path (ADR 0006).
// The fields are public fields on purpose: a property with a setter would become an option.
public sealed class MadeCommand : StarterCommand, IDisposable
{
    public readonly string Origin;
    public int Disposed;

    private MadeCommand(string origin) => Origin = origin;

    public static new IStarterCommand GetInstance<TCommand>() where TCommand : IStarterCommand => new MadeCommand("GetInstance");

    public override Delegate HandlingMethod => () => 0;

    public void Dispose() => Disposed++;
}
