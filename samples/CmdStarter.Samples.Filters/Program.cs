// Sample: command filters for logging and confirmation around every command.

using CyberInternauts.CmdStarter;

namespace CyberInternauts.CmdStarter.Samples.Filters;

public static class Program
{
    public static Task<int> Main(string[] args) => new Starter()
        .AddFilter<LoggingFilter>()
        .AddFilter<ConfirmationFilter>()
        .Start(args);
}
