using System.CommandLine;
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

        Option<bool>? yesOption = null;
        Command? command = context.Command;
        while (command is not null && yesOption is null)
        {
            yesOption = command.Options
                .OfType<Option<bool>>()
                .SingleOrDefault(option => string.Equals(option.Name, "--yes", StringComparison.Ordinal));
            command = command.Parents.FirstOrDefault() as Command;
        }

        if (yesOption is null)
        {
            throw new InvalidOperationException("The --yes option is not registered.");
        }

        if (context.ParseResult.GetValue(yesOption))
        {
            return await next();
        }

        Console.Error.WriteLine("Confirmation required: rerun with --yes.");
        return 3;
    }
}
