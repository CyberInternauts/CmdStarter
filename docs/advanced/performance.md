# Performance

This page gives the startup and execution measurements of CmdStarter taken with BenchmarkDotNet, and how to run them again.

## Conditions

Date: 2026-11-04.

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9818)
Intel Core i7-10700KF CPU 3.80GHz (Max: 3.79GHz), 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-PQAZJI : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
```

- Warm job (`StartupBenchmarks`): 3 warmup iterations and 10 measured iterations in one benchmark process.
- Cold-start job (`ColdStartBenchmarks`): 10 new processes, one invocation each, no warmup.

The numbers depend on the machine and are only comparable with each other.

## Results

### Startup and execution

| Method                                | Mean         | Error       | StdDev      | Gen0    | Gen1   | Allocated |
|-------------------------------------- |-------------:|------------:|------------:|--------:|-------:|----------:|
| DiscoverAllAssemblies                 | 1,621.155 μs | 127.8405 μs |  76.0758 μs | 58.5938 | 7.8125 | 506.55 KB |
| DiscoverOneAssembly                   |   664.237 μs | 211.8950 μs | 126.0953 μs | 27.3438 | 1.9531 | 225.29 KB |
| DefinedTree                           |   827.590 μs | 109.2125 μs |  64.9906 μs | 42.9688 | 5.8594 | 361.39 KB |
| ExecuteCommand                        |     5.575 μs |   0.1445 μs |   0.0956 μs |  1.2131 | 0.0076 |   9.95 KB |
| ExecuteCommandWithDependencyInjection |     5.590 μs |   0.2144 μs |   0.1122 μs |  1.2207 |      - |   10.1 KB |

BenchmarkDotNet reported:

- `ExecuteCommandWithDependencyInjection`: minimum iteration time 89.083 ms, below the 100 ms BenchmarkDotNet recommends.
- Outliers removed: `DiscoverAllAssemblies` 1, `DiscoverOneAssembly` 1, `DefinedTree` 1, `ExecuteCommandWithDependencyInjection` 2; none for `ExecuteCommand`.

### Cold start

| Method           | Mean     | Error   | StdDev  |
|----------------- |---------:|--------:|--------:|
| FirstInstantiate | 137.8 ms | 6.98 ms | 4.62 ms |

BenchmarkDotNet reported:

- `FirstInstantiate`: 2 outliers detected (144.59 ms, 146.68 ms), not removed.

## What is measured

The measures use 12 class commands on three levels placed with `[Parent<T>]`, each with two options (`--count`, `--label`) and one argument; and 12 method commands in two classes, six top-level and six in the `tools` group. The handlers write nothing and return 0. Every measure uses a `Starter` whose `Namespaces` is `CyberInternauts.CmdStarter.Benchmarks.Commands`.

- `DiscoverAllAssemblies`: a new `Starter`, then `InstantiateCommands()`, searching every assembly loaded in the benchmark process.
- `DiscoverOneAssembly`: the same, with `Assemblies` reduced to the benchmark assembly.
- `DefinedTree`: the same, with the 12 class commands placed by `UseTree`; method commands are still discovered.
- `ExecuteCommand`: a `Starter` built once in a `GlobalSetup` method, then `Start("beta beta-two --count 1 value")` for each invocation.
- `ExecuteCommandWithDependencyInjection`: a `CmdStarterBuilder` application built once in a `GlobalSetup` method, with a transient service injected into the `beta-two` command, then `RunAsync("beta beta-two --count 1 value")` for each invocation.
- `FirstInstantiate`: the first `InstantiateCommands()` of a new process, JIT and assembly loading included, searching every loaded assembly.

Global options containers are found by `GlobalOptionsManager` through a static cache shared by every `Starter` of the process, so the warm measures pay that search once per process, while `FirstInstantiate` includes it.

## After the activation cache

This second series was taken after the lookup of each command type's `GetInstance` method was cached (it was computed at every execution), with the same machine and the same jobs as above; the factory of `ActivatorUtilities` is not cached (a type with two constructors would no longer be created).

The conditions are those in [Conditions](#conditions).

### Startup and execution

| Method                                | Mean         | Error       | StdDev      | Gen0    | Gen1   | Allocated |
|-------------------------------------- |-------------:|------------:|------------:|--------:|-------:|----------:|
| DiscoverAllAssemblies                 | 1,626.578 μs | 110.8445 μs |  57.9738 μs | 62.5000 | 7.8125 | 510.63 KB |
| DiscoverOneAssembly                   |   635.556 μs | 251.1641 μs | 166.1295 μs | 25.3906 | 3.9063 | 223.23 KB |
| DefinedTree                           |   808.635 μs | 112.6081 μs |  67.0113 μs | 42.9688 | 7.8125 |  365.2 KB |
| ExecuteCommand                        |     4.550 μs |   0.1237 μs |   0.0818 μs |  1.2054 | 0.0076 |    9.9 KB |
| ExecuteCommandWithDependencyInjection |     4.953 μs |   0.1661 μs |   0.1099 μs |  1.2283 | 0.0076 |  10.05 KB |

BenchmarkDotNet reported:

- `DiscoverAllAssemblies`: 2 outliers removed (1.87 ms, 2.04 ms).
- `DefinedTree`: 1 outlier removed (1.13 ms).
- None for the other measures.

### Cold start

| Method           | Mean     | Error   | StdDev  |
|----------------- |---------:|--------:|--------:|
| FirstInstantiate | 137.3 ms | 2.96 ms | 1.96 ms |

BenchmarkDotNet reported: none.

The two series were taken at different times on the same machine, so part of a difference can be noise.

## How to run

```powershell
dotnet run -c Release --project benchmarks/CmdStarter.Benchmarks -- --filter * --exporters github
```

The reports are written to `BenchmarkDotNet.Artifacts/results/`.
