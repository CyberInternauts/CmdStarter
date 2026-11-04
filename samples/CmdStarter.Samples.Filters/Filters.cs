using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Samples.Filters;

public sealed class LoggingFilter : ICommandFilter
{
    public async Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next)
    {
        var exitCode = await next();
        Console.Error.WriteLine($"[filter] {context.CommandPath} -> {exitCode}");
        return exitCode;
    }
}

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

        Console.Error.WriteLine("Confirmation required: rerun with --yes.");
        return 3;
    }
}
