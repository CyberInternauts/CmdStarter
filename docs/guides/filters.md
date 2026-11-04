# Filters

A filter wraps the execution of a command. It can run code before and after the command, change the exit code, or refuse to run the command. This guide covers how to write a filter, register it, order several filters, and what a filter can see.

## Write a filter

A filter is a class that implements `ICommandFilter`. Its single method, `InvokeAsync(CommandContext context, Func<Task<int>> next)`, returns the exit code that the caller sees. Calling `next` runs the remaining filters and then the command; the value that `next` returns is the command's exit code. A filter that does not call `next` stops the command from running.

The logging filter of the Filters sample calls `next`, then writes the exit code to the error output:

<!-- snippet: samples/CmdStarter.Samples.Filters/Filters.cs -->
```csharp
public sealed class LoggingFilter : ICommandFilter
{
    public async Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next)
    {
        var exitCode = await next();
        context.Console.Error.WriteLine($"[filter] {context.CommandPath} -> {exitCode}");
        return exitCode;
    }
}
```

## Register filters

`Starter.AddFilter` takes a filter instance, and `AddFilter<TFilter>()` takes a filter type. Both return the `Starter`, so the calls chain:

<!-- snippet: samples/CmdStarter.Samples.Filters/Program.cs -->
```csharp
public static Task<int> Main(string[] args) => new Starter()
    .AddFilter<LoggingFilter>()
    .AddFilter<ConfirmationFilter>()
    .Start(args);
```

With the builder, `CmdStarterBuilder.AddFilter<TFilter>()` adds the type to the `Starter` and also registers it in the service collection as a transient, so the filter's constructor can take services from the container. `CmdStarterOptions.AddFilter<TFilter>()`, used in `AddCmdStarter`, registers the type in the service collection the same way.


## Order

The first registered filter is the outermost. With filters A then B, the code of A before `next` runs first, then the code of B before `next`, then the command. On the way out, the code of B after `next` runs before the code of A.

## Refuse a command

A filter that does not call `next` decides that the command does not run. The confirmation filter of the sample refuses `deploy` unless `--yes` is given:

<!-- snippet: samples/CmdStarter.Samples.Filters/Filters.cs -->
```csharp
public sealed class ConfirmationFilter : ICommandFilter
{
    public async Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next)
    {
        if (!Attribute.IsDefined(context.Member, typeof(RequiresConfirmationAttribute)))
        {
            return await next();
        }

        if (context.ParseResult.GetValue<bool>("--yes"))
        {
            return await next();
        }

        context.Console.Error.WriteLine("Confirmation required: rerun with --yes.");
        return 3;
    }
}
```

The filter reads the `RequiresConfirmationAttribute` attribute on `context.Member`, and the `--yes` option through `context.ParseResult`. Without confirmation it returns 3 without calling `next`, so the command does not run, and `CommandHandled` is not raised. The option `--yes` is a [global option](global-options.md) of the sample.

## Change the exit code

The value that a filter returns is the value that `Start` returns. The command still runs, and `CommandHandled` reports the command's own exit code, not the one the filter returned.

## What a filter sees

`CommandContext` gives the filter these members:

- `Command`: the System.CommandLine command.
- `CommandPath`: the command path, without the root.
- `Member`: the command type for a class command, or the `MethodInfo` for a method command.
- `Instance`: the bound instance, or the instance of the declaring class. It is `null` for a static method.
- `ParseResult`: the parsed command line.
- `Services`: the service provider of the execution's scope. It is `null` without dependency injection.
- `Console`: the console of the execution (see [Command console](command-console.md)).
- `CancellationToken`: the token of the execution.

The chain runs after the command instance is created and its options and arguments are bound, so the filter sees the bound values. This is the same for both command models.

A filter that writes its own output should write to `context.Console`, which is the console of the execution, as the sample does.

## Unavailable commands and errors

A command that the current state makes unavailable is refused before any filter runs, so a filter cannot rescue it. See [Command states](command-states.md).

An exception thrown in a filter is handled like an exception thrown by the command: the execution returns 1. See [Errors and exit codes](errors-and-exit-codes.md).

## Lifetimes

A filter registered by type is created for each execution. CmdStarter first looks for the type in the execution's service scope; if the scope does not have it, CmdStarter creates the filter. A filter that CmdStarter created is disposed after the execution, with `DisposeAsync` if it implements `IAsyncDisposable`, and otherwise with `Dispose`.

A filter registered with the builder can therefore take a scoped service: it gets the instance of the execution that creates it.

## Run the sample

```powershell
dotnet run --project samples/CmdStarter.Samples.Filters -- status
dotnet run --project samples/CmdStarter.Samples.Filters -- deploy
dotnet run --project samples/CmdStarter.Samples.Filters -- deploy --yes
```

`status` writes `ok` and returns 0. `deploy` without `--yes` returns 3 and does not deploy. `deploy --yes` writes `deployed` and returns 0.

## See also

- Examples: [Filters](../../samples/CmdStarter.Samples.Filters/Program.cs).
- Neighbouring pages: [Command states](command-states.md), [Command console](command-console.md), [Errors and exit codes](errors-and-exit-codes.md), [Global options](global-options.md), [Dependency injection](../concepts/dependency-injection.md).
- ADR: [0019, command filters](../adr/0019-command-filters.md).
