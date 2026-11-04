# 0029. Trimming and Native AOT, Second Evaluation

Status: Accepted
Date: 2026-11-04

## Context

[ADR 0025](0025-trimming-and-native-aot.md) measured that version 2.0 supported neither trimming nor Native AOT: the trimmer removed the command classes (`NoCommandFoundException`), and the native runtime refused `Assembly.GetReferencedAssemblies` (`PlatformNotSupportedException`). It named a source generator as the only way out, and the next two decisions built it: [ADR 0027](0027-command-catalog.md), the command catalog that the package generates at compile time, and [ADR 0028](0028-typed-symbol-factories.md), the typed factories of symbols that make `Option<T>`, `Argument<T>` and `ParseResult.GetValue<T>` exist statically for value types. This decision measures again, with the same method automated, and decides.

## Method

The script `measure-trimming.ps1`, at the root of the repository, is the method of ADR 0025 made repeatable. It is not part of `dotnet test`.

- It builds the solution in Release, which writes the package `cints.CmdStarter` in `artefacts/`.
- It recreates, outside the repository, in `$env:TEMP\CmdStarter-trimming`, four applications that reference the package and nothing else of the repository: `HelloWorld` and `DevOpsTool`, which hold a copy of the sources of the samples, and `Lists` and `Tree`, which the script writes itself. The criterion is functional parity: what an application does on CoreCLR, it must do trimmed and as a native executable. `Lists` and `Tree` exist because the two samples leave features unexercised. `Lists` has:
  - a command with a `List<string>` option, an `int[]` option and a `string[]` argument (`show`), whose handler writes the three values on one line;
  - a service provider set on the `Starter`, with one registered service, `IGreeter`, and no command registered in it. The command `greet`, whose constructor takes `IGreeter`, is therefore created by `ActivatorUtilities`;
  - a filter registered by type with `AddFilter<T>`, created by `ActivatorUtilities` too, which writes on the error output the line of the constructor that served ("filter: Hello, filter!" for the constructor that takes `IGreeter`, "filter: no container" for the parameterless one; the measured filter has a parameterless constructor too, because a command created by its own `GetInstance` runs without the service provider, [ADR 0006](0006-dependency-injection.md));
  - a command `made` that declares its own static `GetInstance`, so that it is created through the search of that method, `MakeGenericMethod` on a class, then `Invoke`; its handler writes "made by GetInstance", read from a field that only that method sets;
  - a class with a method marked `[Command]` that has an `[Option] List<string> tags` parameter and an `int count` parameter (`tag`): the options and arguments of a method, `MethodInfo.Invoke`, and the test of the interfaces on a parameter.
  `Tree` places its commands with `Starter.UseTree`, one command under the group `tools`; with its first argument `ct`, it sets `Starter.CommandsTypes` to a type of the catalog instead.
- **F1, the cache of NuGet.** The version of the package is fixed, so the global cache of the user would serve an old package at the second run. The script restores in its own packages folder, deleted at each run, and never touches the cache of the user. The table has a row with the SHA-256 of the package of `artefacts/` and the one of the package restored: they must be equal, and they were in every measure below.
- It publishes each application twice, with `-c Release -r win-x64 --self-contained` and `-p:PublishTrimmed=true` or `-p:PublishAot=true`, adding the folder of `vswhere.exe` to the `PATH` of the process for Native AOT (ADR 0025 found it was needed). It runs each executable, and records the exit code, the first line of the output and the first line of the error output. The applications have no `TreatWarningsAsErrors`, so that the warnings of the publication do not stop the measure.
- **F2, the place of the table.** It is written in `coverage/trimming/trimming.md`, which `.gitignore` excludes. `measure-coverage.ps1` deletes `coverage/report/` at each run, so the table could not live there.
- The static analysis is the one of ADR 0025: `dotnet build CmdStarter.Lib/CmdStarter.Lib.csproj -c Release -nologo -p:IsAotCompatible=true -p:TreatWarningsAsErrors=false -p:GeneratePackageOnBuild=false --no-incremental`. Warnings are counted as distinct (code, file, line).

The commands that are run: `HelloWorld.exe --to Bob` (expected `Hello, Bob!`); `DevOpsTool.exe service deploy api --version 1.2.3` (expected `Deploying api 1.2.3 to dev`), `DevOpsTool.exe --help` (code 0, a non-empty output, which shows that the descriptions and options read from the attributes survive) and `DevOpsTool.exe service deploy` (a parse error, exit code from 1 to 255, no unhandled exception); `Lists.exe show a b --names x --names y --numbers 1 2` (expected `names=x,y values=a,b numbers=1,2`), `greet --who Bob` (`Hello, Bob!`), `made` (`made by GetInstance`, and `filter: no container` on the error output), `tag red 3 --tags a --tags b` (`label=red tags=a,b count=3`), and `show c --names z --numbers 3` with `filter: Hello, filter!` as the first line of the error output; `Tree.exe tools show --label x` (`label=x`), `Tree.exe tools --help` (code 0, a non-empty output) and `Tree.exe ct show --label y` (`label=y`, with `CommandsTypes`).

## Results

All measures are of 2026-11-04, on the same machine, win-x64, with the same SDK 10.0.403. The times are those of the machine.

| Measure | Package | Publications | Runs |
| --- | --- | --- | --- |
| 12:29:30 to 12:30:45 | catalog and factories, nothing else | 4, exit code 0, warnings IL2104 and IL3053 | 0 of 8 |
| 12:43:12 to 12:44:00 | with the switch of the dynamic code (below) | 4, exit code 0, warnings IL2104 and IL3053 | 8 of 8 |
| 12:56:31 to 12:57:19 | with the annotations | 4, exit code 0, no warning | 8 of 8 |
| 13:02:22 to 13:03:50 | with `Lists` | 6, exit code 0, no warning | 10 of 10 |
| 13:16:15 to 13:17:20 | with `IsTrimmable` and `IsAotCompatible` set | 6, exit code 0, no warning | 10 of 10 |
| 13:44:05 to 13:45:57 | with `Lists` extended, `Tree` and the diagnostic CMDS0016 | 8, exit code 0, no warning | 24 of 24 |

**The first measure, 8 failures with three causes.**

- Trimmed, both applications (`-532462766`, unhandled exception): `System.IO.FileNotFoundException`, "Could not load file or assembly 'System.Runtime, Version=8.0.0.0'", raised by `Assembly.GetCustomAttribute<CommandCatalogAttribute>` in the read of the catalog. The commands were found, as the catalog works; but the loop of the search reads the attribute on every loaded assembly, and one of them is `System.CommandLine.resources`, the French satellite assembly of System.CommandLine (the machine culture is fr-CA). It references `System.Runtime` 8.0.0.0, which the trimmed application does not have. A probe application outside the repository, which created a `Starter` and read the attribute on each assembly of the AppDomain, gave the name of the assembly; the same application not trimmed read all of them.
- Native AOT, HelloWorld (`-1073740791`): `System.PlatformNotSupportedException` in `Assembly.GetReferencedAssemblies()`, called by `Starter.DiscoverCommandsTypes` on the assemblies of the framework, which have no catalog and were scanned (the limit D6 of ADR 0027). HelloWorld creates `new Starter()` without `Assemblies`.
- Native AOT, DevOpsTool (`-1073740791`): `System.InvalidOperationException`, "There is no metadata token available for the given member", in `Starter.AddMethodCommands`, which sorted the methods of a class by `MethodInfo.MetadataToken` to keep their declaration order.

**What fixed them** (the second measure, 8 of 8): the attribute of the catalog is read only on the library or on an assembly that references it, and a failure to load the attributes (`FileNotFoundException`, `FileLoadException`, `TypeLoadException`, and no other exception) makes the assembly count as one without catalog; without dynamic code (`RuntimeFeature.IsDynamicCodeSupported` false, as under Native AOT) only the catalogs are read, no assembly is scanned and `GetReferencedAssemblies` is never asked; and the methods keep the order of `GetMethods` without dynamic code. The references of a searched assembly are asked once.

**The static analysis.**

| Code | ADR 0025 | Before the annotations | Now |
| --- | ---: | ---: | ---: |
| `IL2026` | 5 | 3 | 0 |
| `IL2060` | 3 | 1 | 0 |
| `IL2067` | 4 | 5 | 0 |
| `IL2070` | 7 | 7 | 0 |
| `IL2072` | 10 | 6 | 0 |
| `IL2075` | 3 | 3 | 0 |
| `IL2091` | 1 | 2 | 0 |
| `IL3050` | 9 | 2 | 0 |
| **Total** | **42** | **29** | **0** |

With the two flags set, the build of the solution is the gate: `TreatWarningsAsErrors` is on, and `dotnet build CmdStarter.sln` gives no warning. The package `cints.CmdStarter` now carries the metadata `IsTrimmable` and `IsAotCompatible` in `CmdStarter.Lib.dll` (read by reflection on the restored package). The publications no longer report `IL2104` or `IL3053` ("Assembly 'CmdStarter.Lib' produced trim/AOT analysis warnings"): the library has no analysis warning left.

**The final measure** (13:44:05 to 13:45:57, 112 s), taken on the package that carries the diagnostic CMDS0016 of the generator, the last change. SHA-256 of the package built and of the package restored: `EBF78995B1F47E10A771B1851B8AEF4261C6490C24D5187B566AA7D26549E83A`, the same. (The measure of 13:16:15, with the first version of `Lists`, gave 10 of 10 on the package `3FAB426A4803977720AEFDD917DDC9D701245C63BDF49DCE17E5896B2DF281FD`.) Publications, exit code 0, no warning:

| Application | Mode | Folder (bytes) | Executable (bytes) |
| --- | --- | ---: | ---: |
| HelloWorld | Trimmed | 21498346 | 162304 |
| HelloWorld | Native AOT | 27743232 | 4989952 |
| DevOpsTool | Trimmed | 21616624 | 162304 |
| DevOpsTool | Native AOT | 29312000 | 5231616 |
| Lists | Trimmed | 21591325 | 162304 |
| Lists | Native AOT | 29106688 | 5198336 |
| Tree | Trimmed | 21506048 | 162304 |
| Tree | Native AOT | 27952128 | 5026816 |

Runs, trimmed and Native AOT alike, 24 of 24 as expected: `Hello, Bob!` (0); `Deploying api 1.2.3 to dev` (0); `--help` (0, a non-empty output); `service deploy` (1, "Argument obligatoire manquant pour la commande : 'deploy'."); for `Lists`, `names=x,y values=a,b numbers=1,2` (0), `Hello, Bob!` (0), `made by GetInstance` with `filter: no container` (0), `label=red tags=a,b count=3` (0) and `names=z values=c numbers=3` with `filter: Hello, filter!` (0); for `Tree`, `label=x` (0), the help of the group (0, a non-empty output) and `label=y` with `CommandsTypes` (0). Run by hand on the two modes of DevOpsTool, eight more commands (the help of the three levels, an unknown service giving 3, an unknown environment giving 2, a rollback in dry-run, `status`, a deployment with several options) gave the same text and the same code.

**The cold start.** The series of BenchmarkDotNet taken from 13:07:58 to 13:09:27 ([Performance](../advanced/performance.md)) answers the reserve of ADR 0027: `FirstInstantiate` gives 147.6 ± 9.05 ms and `FirstInstantiateByScan` 143.9 ± 7.09 ms. The intervals overlap largely: no measurable difference. The twins of the discovery overlap too.

## Decision

The criterion is functional parity: a feature that works on CoreCLR must work trimmed and as a native executable, and each exclusion below is a feature that is lost, named as such.

Version 2.0 supports trimming and Native AOT for an application that has a command catalog, which the package generates by default, on win-x64, as measured.

| Application | Trimmed | Native AOT |
| --- | --- | --- |
| HelloWorld (a class command, `new Starter()` without `Assemblies`) | supported | supported |
| DevOpsTool (class and method commands, Microsoft DI, global options) | supported | supported |
| Lists (list options and an array argument; commands created by `ActivatorUtilities`; a filter registered by type; a command created by its own `GetInstance`; a method command with an `[Option]` list parameter) | supported | supported |
| Tree (`Starter.UseTree` with a group; `Starter.CommandsTypes` set to a type of the catalog) | supported | supported |

`IsTrimmable` and `IsAotCompatible` are set to `true` in `CmdStarter.Lib.csproj` and in `CmdStarter.Hosting.csproj`. `CmdStarter.Hosting` is flagged by static analysis only (0 IL warning, the same command as for the library); no application was measured.

The decision of ADR 0025 is replaced: its status becomes `Superseded by` this ADR.

## Annotations and suppressions

The remedies were applied in this order: an annotation `DynamicallyAccessedMembers` where the member is read; a suppression with a justification that names the catalog where a `Type` leaves a collection and loses its annotation, or where the fallback by reflection only runs without a catalog; never `RequiresUnreferencedCode` or `RequiresDynamicCode` on a public method that `Start` calls, because the warning would reach every application.

**Annotations.** The catalog already annotated its entries with `All` (`AddCommand`, `AddMethodCommands`, `AddGlobalOptions`). Added: `PublicMethods` on `features` of the two constructors of `AutoCompleteAttribute` and on the type parameter of `AutoCompleteAttribute<T>`; `PublicMethods` on `commandType` of `Helper.FindGetInstanceMethod`, `PublicProperties` on `type` of `Helper.GetProperties(Type)`; `PublicConstructors` on the type parameter of the three `AddFilter<TFilter>` (`Starter`, `CmdStarterOptions`, `CmdStarterBuilder`) and on `FilterRegistration.FilterType`; `PublicParameterlessConstructor` on the type parameter of `ICommandCatalogBuilder.AddSymbolType<T>` and of `SymbolFactory<T>`, which keeps the parameterless constructor of a `struct` that has one; and on the internal parameters that carry a type (`Activate`, `ActivateFromServices`, `AddMethodCommands`, `CreateGlobalOptions`, `CreateSample`, `Loader.LoadOptions`, `MethodCommandsLoader`, `Helper.SearchGetInstanceMethod`, `Helper.DeclaresGetInstance`). On the public members, they have no effect at run time.

**The funnel.** `KeptTypes.Of(Type)` returns its argument with the annotation `All`, and it carries one suppression, IL2068. The commands, the classes of method commands, the containers and the filters are registered through annotated APIs, then kept in collections (`CommandsTypes`, `MethodCommandsTypes`, `GlobalOptionsTypes`, the filters), which loses the annotation. Every place where a `Type` leaves a collection for a member that needs one goes through it. Its justification: "Every type that reaches this method was registered through an annotated API: the command catalog (AddCommand, AddMethodCommands and AddGlobalOptions keep all the members of the type), AddFilter (keeps the public constructors), or it is a type that the application hands to the starter itself, which then answers for it."

**The suppressions** (13 attributes on 10 members, `KeptTypes.Of` excluded):

| Code | Member | Justification |
| --- | --- | --- |
| IL2087 | `StarterCommand.GetInstance<TCommand>` | TCommand is a command type, and the command catalog keeps all the members of the commands it declares, the public methods that a command inherits included. |
| IL2091 | `IStarterCommand.GetInstance<TCommand>` | TCommand is a command type, and the command catalog keeps all the members of the commands it declares, their parameterless constructor included. |
| IL2026 | `CatalogReader.ReferencesLibrary` | The assemblies searched are those of the application, and an assembly that uses the library keeps its reference to it. The references are asked only when dynamic code is supported, never under Native AOT. |
| IL2071 | `SymbolFactories.CreateFallback` | The fallback only runs for a value type that no command catalog registered, which the generator does not leave out for a type it can write. A type it cannot write is not supported under trimming; on CoreCLR the type is kept, because the application uses it. |
| IL3050 | `SymbolFactories.CreateFallback` | The fallback only runs for a value type that no command catalog registered: the generator registers the value types that the commands use, so that their factories exist in the application. Under Native AOT, a value type it could not register is not supported. |
| IL2070, IL2075 | `Loader.ImplementsIList` | The type of an option comes from a property or a parameter of a command that the command catalog keeps with all its members, and the interfaces of a type that the application uses are kept with it. |
| IL2075 | `Helper.SearchGetInstanceMethod` | GetMethod reads the public methods of the base types of a command, and a command type that is kept (the command catalog keeps all its members) keeps the public methods that it inherits. |
| IL2060 | `Helper.SearchGetInstanceMethod` | The generic method is IStarterCommand.GetInstance, instantiated over a command class, which is a reference type: the instantiation shares the code of the canonical form and needs nothing of its own. |
| IL3050 | `Helper.SearchGetInstanceMethod` | The generic method is IStarterCommand.GetInstance, instantiated over a command class, which is a reference type: the instantiation shares the code of the canonical form, so no native code has to be generated at run time. |
| IL2072 | `Helper.GetProperties(object)` | The type is the one of an instance that exists, which is a command or a global options container: the command catalog keeps all the members of the types it declares. |
| IL2026 | `Helper.GetLoadableTypes` | The scan of the types of an assembly only runs for an assembly that has no command catalog, which is not supported in a trimmed application: an assembly that has one is read from its catalog, which keeps the types it declares. |

**Without suppression.** The validation context of the validation attributes is built with the constructor that takes a display name, which the trimmer does not flag (it was IL2026). `Helper.FindGetInstanceMethod` no longer uses `GetOrAdd` with a method group: a delegate to a method whose parameter is annotated is a reflection access for the trimmer (IL2111).

## Exclusions

Features that are lost, each named as such:

- **A command type that the catalog does not declare.** With the generator active, the catalog declares every command class of the assembly, so `Starter.UseTree` and `Starter.CommandsTypes` on those types are supported, and both are measured (`Tree`: `UseTree` with a group, and `CommandsTypes` set to a type of the catalog). What is lost is a type that no catalog declares: a catalog written by hand that omits it, or the generator turned off. Nothing keeps its members under trimming, and `KeptTypes.Of` states the opposite only for the types of a catalog.
- **An assembly without a catalog.** Under Native AOT, an assembly without a catalog is not scanned, so its commands are not found. With `Starter.UseCommandCatalogs` set to `false`, the search throws a `CmdStarterException` ("Command catalogs are off (UseCommandCatalogs = false), and this runtime does not support the reflection scan: turn catalogs on."), where it would have blamed a filter. Under trimming on CoreCLR, the scan still exists, but nothing keeps the types of an assembly that has no catalog.
- **A value type that the generator cannot write as a type argument** (ref-like types, pointers, inaccessible types, `[Obsolete(..., true)]`, `[Experimental]`, see ADR 0028), and a value type used only in a handler whose `HandlingMethod` cannot be read at compile time (CMDS0015): an option or an argument of such a type uses the fallback by reflection, which Native AOT does not support, because the instantiation may not exist in the executable. The generator now says so at compile time: CMDS0016 (severity `Info`, and not `Warning`, for the reason of CMDS0015 in ADR 0028: under `TreatWarningsAsErrors`, a warning would break the build of a valid application) names each such value type once, on the first member that uses it, with the reason of CMDS0014 and the advice to make the type reachable from a top-level class. It is not issued for a reference type, which shares its code, nor for ref-like types and pointers, which are not the type of an option on any platform. No project of the solution is concerned: the count of CMDS0016 in the projects that carry the generator is 0.

Not measured (no loss shown, no proof either):

- **The order of the methods under Native AOT** is the one of `GetMethods`; it was not tried with a class whose methods are not in the order of their declaration. In DevOpsTool, the help gives the declaration order in both modes.
- **The other platforms.** Only win-x64 was measured.
- **The satellite assembly.** The failure of the first measure is that of a culture: the satellite assembly of System.CommandLine for the machine culture was loaded and its attributes could not be resolved. The catalog is now read only on the library or on an assembly that references it, and a failure to load the attributes is not an error. A culture other than fr-CA was not tried.
- **`CmdStarter.Hosting`** is flagged by static analysis only (0 IL warning), and no application was measured.

No longer excluded: the `[Option]` parameter of a method command (`tag` in `Lists`, trimmed and Native AOT), `ActivatorUtilities.CreateInstance` for a command that the container does not have and for a filter registered by type (`greet` and the filter of `Lists`), and the static `GetInstance` of a command (`made` in `Lists`) are measured and work.

## Consequences

- The package carries `IsTrimmable` and `IsAotCompatible`. The build of the library and of Hosting is itself the gate: a new analysis warning breaks the build.
- The annotations of the public members have no effect at run time. An implementation of `ICommandCatalogBuilder` that analyzes trimming must take the same annotations on `AddSymbolType<T>`. The migration guide says so.
- `Starter.UseCommandCatalogs` set to `false` throws on a runtime without dynamic code.
- The tests of the library do not run the publications. The script is the proof, and a change that could break trimming or Native AOT is checked by running it; the build with the flags only covers what the analyzers can see.
- The cold start does not depend on the catalogs.
