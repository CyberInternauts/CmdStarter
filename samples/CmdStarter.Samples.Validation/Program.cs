// Sample: parse errors handled by System.CommandLine, plus DataAnnotations validation attributes.

using CyberInternauts.CmdStarter;

namespace CyberInternauts.CmdStarter.Samples.Validation;

public static class Program
{
    public static Task<int> Main(string[] args) => new Starter().Start(args);
}

