using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using CyberInternauts.CmdStarter.Benchmarks.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter.Benchmarks;

/// <summary>In-process measures of command discovery, tree building, instantiation and execution.</summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class StartupBenchmarks
{
    private Starter executionStarter = null!;
    private CmdStarterApp dependencyInjectionApp = null!;

    /// <summary>Builds the starters that the execution measures reuse.</summary>
    [GlobalSetup]
    public void Setup()
    {
        executionStarter = BenchmarkSetup.CreateStarter();
        executionStarter.InstantiateCommands();

        var builder = new CmdStarterBuilder()
            .AddCommandsFromAssembly(typeof(StartupBenchmarks).Assembly)
            .ConfigureStarter(starter => starter.Namespaces = ImmutableList.Create(BenchmarkSetup.CommandsNamespace));
        builder.Services.AddTransient<IBenchmarkService, BenchmarkService>();
        dependencyInjectionApp = builder.Build();
    }

    /// <summary>Releases the application built by <see cref="Setup"/>.</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        dependencyInjectionApp.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <summary>Instantiates the commands of a new starter, searching every assembly loaded in the process.</summary>
    /// <returns>The number of root subcommands.</returns>
    [Benchmark]
    public int DiscoverAllAssemblies()
    {
        var starter = BenchmarkSetup.CreateStarter();
        starter.InstantiateCommands();
        return starter.RootCommand.Subcommands.Count;
    }

    /// <summary>Instantiates the commands of a new starter, searching only the assembly of the measures.</summary>
    /// <returns>The number of root subcommands.</returns>
    [Benchmark]
    public int DiscoverOneAssembly()
    {
        var starter = BenchmarkSetup.CreateStarter();
        starter.Assemblies = ImmutableList.Create(typeof(StartupBenchmarks).Assembly);
        starter.InstantiateCommands();
        return starter.RootCommand.Subcommands.Count;
    }

    /// <summary>Instantiates the commands of a new starter whose class commands are placed with <see cref="Starter.UseTree"/>.</summary>
    /// <returns>The number of root subcommands.</returns>
    [Benchmark]
    public int DefinedTree()
    {
        var starter = BenchmarkSetup.CreateStarter();
        starter.UseTree(BenchmarkSetup.DefineTree);
        starter.InstantiateCommands();
        return starter.RootCommand.Subcommands.Count;
    }

    /// <summary>Runs a second-level command on a starter that is already built.</summary>
    /// <returns>The exit code.</returns>
    [Benchmark]
    public Task<int> ExecuteCommand() => executionStarter.Start(BenchmarkSetup.SecondLevelCommandLine);

    /// <summary>Runs a second-level command on an application built with Microsoft dependency injection.</summary>
    /// <returns>The exit code.</returns>
    [Benchmark]
    public Task<int> ExecuteCommandWithDependencyInjection() => dependencyInjectionApp.RunAsync(BenchmarkSetup.SecondLevelCommandLine);
}
