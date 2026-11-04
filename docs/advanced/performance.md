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
- `DiscoverAllAssembliesByScan` and `DiscoverOneAssemblyByScan` (third series): the same two measures with `Starter.UseCommandCatalogs` set to `false`.
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

## With command catalogs

These two series were taken on 2026-11-04, one after the other, after the [command catalog](source-generator.md) was added, with the same machine, the same jobs and the same conditions as in [Conditions](#conditions). The first was taken from 11:16:45 to 11:18:15, the second from 11:22:35 to 11:24:03. The assembly of the measures now has a generated catalog, because the project references the analyzer package. Two measures were added, `DiscoverAllAssembliesByScan` and `DiscoverOneAssemblyByScan`: they are `DiscoverAllAssemblies` and `DiscoverOneAssembly` with `Starter.UseCommandCatalogs` set to `false`, so that every assembly is scanned by reflection.

What the code does, and what the measures cover:

- Every searched assembly has its catalog attribute read. Without `Assemblies`, every loaded assembly pays that read, and only an assembly that has a catalog avoids its scan.
- Without `Assemblies`, `Starter` searches every loaded assembly. Only the assembly of the measures has a catalog; the assemblies of the framework and the others are scanned in both variants, `GetReferencedAssemblies` included. `DiscoverOneAssembly`, with `Assemblies` reduced to the assembly of the measures, is the measure that shows what a catalog replaces.
- The global options containers are found once per process, so between the two variants only the discovery of the commands differs.

### Series of 11:16:45 to 11:18:15

#### Startup and execution

| Method                                | Mean         | Error       | StdDev      | Gen0    | Gen1    | Gen2    | Allocated |
|-------------------------------------- |-------------:|------------:|------------:|--------:|--------:|--------:|----------:|
| DiscoverAllAssemblies                 | 2,065.198 μs | 261.4089 μs | 155.5603 μs | 85.9375 | 85.9375 | 85.9375 | 890.43 KB |
| DiscoverOneAssembly                   |   538.299 μs | 159.4757 μs |  94.9014 μs | 25.3906 |  3.9063 |       - | 220.07 KB |
| DiscoverAllAssembliesByScan           | 1,837.083 μs | 104.6353 μs |  54.7263 μs | 85.9375 | 85.9375 | 85.9375 | 862.64 KB |
| DiscoverOneAssemblyByScan             |   702.277 μs | 246.9527 μs | 163.3439 μs | 27.3438 |  3.9063 |       - | 224.17 KB |
| DefinedTree                           |   903.380 μs |  23.6361 μs |  12.3621 μs | 48.8281 |  7.8125 |       - | 410.53 KB |
| ExecuteCommand                        |     4.849 μs |   0.1722 μs |   0.1139 μs |  1.2054 |  0.0076 |       - |    9.9 KB |
| ExecuteCommandWithDependencyInjection |     4.899 μs |   0.1617 μs |   0.1070 μs |  1.2283 |  0.0076 |       - |  10.05 KB |

BenchmarkDotNet reported:

- `DiscoverAllAssemblies`: 1 outlier removed (2.60 ms).
- `DiscoverOneAssembly`: 1 outlier removed (917.15 μs).
- `DiscoverAllAssembliesByScan`: 2 outliers removed (2.15 ms, 2.64 ms).
- `DefinedTree`: 2 outliers removed (969.11 μs, 1.11 ms).
- None for the other measures.

#### Cold start

| Method           | Mean     | Error   | StdDev  |
|----------------- |---------:|--------:|--------:|
| FirstInstantiate | 148.3 ms | 9.24 ms | 6.11 ms |

BenchmarkDotNet reported: none.

### Series of 11:22:35 to 11:24:03

#### Startup and execution

| Method                                | Mean         | Error       | StdDev      | Gen0    | Gen1    | Gen2    | Allocated |
|-------------------------------------- |-------------:|------------:|------------:|--------:|--------:|--------:|----------:|
| DiscoverAllAssemblies                 | 2,032.974 μs | 332.3952 μs | 197.8031 μs | 85.9375 | 85.9375 | 85.9375 | 890.43 KB |
| DiscoverOneAssembly                   |   601.921 μs | 244.0300 μs | 161.4107 μs | 25.3906 |  3.9063 |       - | 220.07 KB |
| DiscoverAllAssembliesByScan           | 1,844.305 μs |  97.1134 μs |  50.7922 μs | 85.9375 | 85.9375 | 85.9375 | 862.64 KB |
| DiscoverOneAssemblyByScan             |   681.376 μs | 256.0471 μs | 169.3593 μs | 27.3438 |  3.9063 |       - | 224.17 KB |
| DefinedTree                           |   862.431 μs |  63.8533 μs |  37.9981 μs | 48.8281 |  7.8125 |       - | 410.52 KB |
| ExecuteCommand                        |     4.583 μs |   0.1377 μs |   0.0720 μs |  1.2054 |  0.0076 |       - |    9.9 KB |
| ExecuteCommandWithDependencyInjection |     5.014 μs |   0.1027 μs |   0.0679 μs |  1.2283 |  0.0076 |       - |  10.05 KB |

BenchmarkDotNet reported:

- `DiscoverAllAssemblies`: 1 outlier removed (2.65 ms).
- `DiscoverAllAssembliesByScan`: 2 outliers removed (2.05 ms, 2.20 ms).
- `DefinedTree`: 1 outlier removed (1.06 ms).
- `ExecuteCommand`: 2 outliers removed, 3 outliers detected (4.41 μs, 4.71 μs, 4.79 μs).
- None for the other measures.

#### Cold start

| Method           | Mean     | Error   | StdDev  |
|----------------- |---------:|--------:|--------:|
| FirstInstantiate | 147.5 ms | 7.39 ms | 4.89 ms |

BenchmarkDotNet reported: 1 outlier detected (156.76 ms), not removed.

### Reading the two series

In each series, the error intervals of `DiscoverOneAssembly` and `DiscoverOneAssemblyByScan` overlap, and so do those of `DiscoverAllAssemblies` and `DiscoverAllAssembliesByScan`: no measurable gain. `FirstInstantiate` has no twin by scan. Its error interval in each of the two series overlaps that of the series before catalogs (137.3 ms, with an error of 2.96 ms): no measurable gain.

## How to run

```powershell
dotnet run -c Release --project benchmarks/CmdStarter.Benchmarks -- --filter * --exporters github
```

The reports are written to `BenchmarkDotNet.Artifacts/results/`.
