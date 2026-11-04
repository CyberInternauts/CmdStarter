using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace CyberInternauts.CmdStarter.Benchmarks;

/// <summary>Measures of the first use of the library in a new process.</summary>
[SimpleJob(RunStrategy.ColdStart, launchCount: 10, warmupCount: 0, iterationCount: 1)]
public class ColdStartBenchmarks
{
    /// <summary>Instantiates the commands of a new starter for the first time in the process, searching every loaded assembly.</summary>
    /// <returns>The number of root subcommands.</returns>
    [Benchmark]
    public int FirstInstantiate()
    {
        var starter = BenchmarkSetup.CreateStarter();
        starter.InstantiateCommands();
        return starter.RootCommand.Subcommands.Count;
    }

    /// <summary>
    /// Like <see cref="FirstInstantiate"/>, but with <see cref="Starter.UseCommandCatalogs"/> off: every assembly is scanned by reflection, as before catalogs.
    /// It is the twin that tells whether the catalogs change the cold start.
    /// </summary>
    /// <returns>The number of root subcommands.</returns>
    [Benchmark]
    public int FirstInstantiateByScan()
    {
        var starter = BenchmarkSetup.CreateStarter();
        starter.UseCommandCatalogs = false;
        starter.InstantiateCommands();
        return starter.RootCommand.Subcommands.Count;
    }
}
