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
}
