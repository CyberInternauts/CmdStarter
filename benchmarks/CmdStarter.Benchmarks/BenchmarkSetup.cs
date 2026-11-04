using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Benchmarks.Commands.Classes;
using CyberInternauts.CmdStarter.Loader;

namespace CyberInternauts.CmdStarter.Benchmarks;

/// <summary>Shared construction of the starters and command lines used by the measures.</summary>
internal static class BenchmarkSetup
{
    /// <summary>The namespace of the measured commands, for both the class commands and the method commands.</summary>
    public const string CommandsNamespace = "CyberInternauts.CmdStarter.Benchmarks.Commands";

    /// <summary>A second-level command line that reaches a handler, with its two options and its argument.</summary>
    public const string SecondLevelCommandLine = "beta beta-two --count 1 value";

    /// <summary>Creates a starter that only sees the measured commands.</summary>
    public static Starter CreateStarter() => new()
    {
        Namespaces = [CommandsNamespace]
    };

    /// <summary>Defines the twelve class commands at the same places as the discovery does.</summary>
    public static void DefineTree(CommandTreeDefinition tree)
    {
        tree
            .Add<Alpha>(alpha => alpha
                .Add<AlphaOne>(one => one
                    .Add<AlphaOneX>()
                    .Add<AlphaOneY>())
                .Add<AlphaTwo>())
            .Add<Beta>(beta => beta
                .Add<BetaOne>(one => one
                    .Add<BetaOneX>())
                .Add<BetaTwo>())
            .Add<Gamma>(gamma => gamma
                .Add<GammaOne>(one => one
                    .Add<GammaOneX>()));
    }
}
