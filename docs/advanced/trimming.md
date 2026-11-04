# Trimming

This page says where CmdStarter stands with trimming. It holds two evaluations, both of 2026-11-04. The first, from the start of the page to its conclusion, was made before the command catalog and the typed factories of symbols existed, and found that trimming did not work. The [second evaluation](#second-evaluation) was made after them and supersedes it: version 2.0 supports trimming for an application that has the command catalog.

## What the library does with reflection

CmdStarter finds and creates commands at run time, so it uses reflection in several places:

- Discovery: `Starter.FindCommandsTypes` looks for command classes in the loaded assemblies. Discovery expects the command classes to still exist in their assembly; a trimmer removes a class that no code references.
- Generic types and methods built at run time (`MakeGenericType` and `MakeGenericMethod`) create the System.CommandLine options and arguments for each property and parameter type, and read option values.
- Handlers and methods are invoked by reflection: a class command's handler through `DynamicInvoke`, a method command through `MethodInfo.Invoke`, and the `GetInstance` methods of the commands.
- Instances are created with `Activator.CreateInstance` when no dependency injection container is used, and with `ActivatorUtilities` when one is.
- Properties, attributes and interfaces are read by reflection: option properties, the `[Parent]` and `[Children]` attributes, and the validation attributes.

## Static analysis

The analysis is the build of the library project with `IsAotCompatible` set on the command line. It reports 42 distinct `IL` warnings. Some positions carry two codes. The build also reports two `NETSDK1210` warnings from the analyzer project; they are not counted.

| Code | Count | Meaning | Files |
| --- | ---: | --- | --- |
| `IL2026` | 5 | a member marked `RequiresUnreferencedCode` is used | GlobalOptionsManager.cs, Reflection/Loader.cs, Starter.cs |
| `IL2060` | 3 | a `MakeGenericMethod` call that cannot be analysed | Binding/CommandBinder.cs, Reflection/Helper.cs |
| `IL2067` | 4 | a `Type` parameter lacks the requirements of `Activator` or `ActivatorUtilities` | Binding/CommandBinder.cs, Starter.cs |
| `IL2070` | 7 | `GetMethod(s)` or `GetProperties` on a `Type` parameter | Attributes/AutoCompleteAttribute.cs, Loader/MethodCommandsLoader.cs, Reflection/Helper.cs |
| `IL2072` | 10 | a `Type` value returned by a method lacks the requirements | Binding/CommandBinder.cs, CmdStarterServiceCollectionExtensions.cs, GlobalOptionsManager.cs, Reflection/Loader.cs, Starter.cs |
| `IL2075` | 3 | `GetMethod` or `GetInterfaces` on a type obtained by reflection | Reflection/Helper.cs, Reflection/Loader.cs |
| `IL2091` | 1 | a generic argument lacks the requirements | Interfaces/IStarterCommand.cs |
| `IL3050` | 9 | `MakeGenericType` or `MakeGenericMethod` (dynamic code) | Binding/CommandBinder.cs, Reflection/Helper.cs, Reflection/Loader.cs |
| **Total** | **42** | | |

## Method

The measures use an application outside the repository. It references the package `cints.CmdStarter` 2.0.0-preview.1, built in `artefacts/`, and its sources are copied from the HelloWorld and DevOpsTool samples. This is the case of a package user.

The publication is not run inside the repository: a property given on the command line is global, so it also reaches the analyzer project, which targets netstandard2.0. That project refuses trimming (NETSDK1124) and Native AOT (NETSDK1207). The analyzer is not a runtime dependency of an application.

```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishTrimmed=true
dotnet publish -c Release -r win-x64 --self-contained -p:PublishAot=true
```

The static analysis is run on the library project with:

```powershell
dotnet build CmdStarter.Lib/CmdStarter.Lib.csproj -c Release -nologo -p:IsAotCompatible=true -p:TreatWarningsAsErrors=false -p:GeneratePackageOnBuild=false --no-incremental
```

Publishing the test applications with trimming gives one warning, `IL2104` ("Assembly 'CmdStarter.Lib' produced trim warnings."), for each of them. The exit code of `dotnet publish` is 0.

## Trimmed applications

Without trimming, the same applications work: `HelloWorld.exe --to Bob` returns 0 and prints `Hello, Bob!`, and `DevOpsTool.exe service deploy api --version 1.2.3` returns 0 and prints `Deploying api 1.2.3 to dev`.

With trimming, both applications publish and then fail when they run:

| Application | Size of the published folder | Command run | Exit code |
| --- | ---: | --- | ---: |
| HelloWorld | 21 501 250 bytes (`HelloWorld.dll`: 4 096 bytes) | `HelloWorld.exe --to Bob` | `-532462766` (`0xE0434352`, unhandled .NET exception) |
| DevOpsTool | 21 611 940 bytes | `DevOpsTool.exe service deploy api --version 1.2.3` | `-532462766` |

The first line of the output of HelloWorld, verbatim:

```text
Unhandled exception. CyberInternauts.CmdStarter.Exceptions.NoCommandFoundException: No command was found after applying the Namespaces filter.
```

The stack goes from `Starter.FindCommandsTypes()` through `BuildTree()`, `InstantiateCommandsCore`, `InstantiateCommandsAsync`, `StartCoreAsync` and `Start(String[])` up to `Program.Main`. For DevOpsTool, the first line is the same exception with the message "No command was found after applying the Assemblies filter.", and the stack goes from `Starter.FindCommandsTypes()` through `CmdStarterServiceCollectionExtensions.AddCmdStarter` and `CmdStarterBuilder.Build()` up to `Program.Main`.

The trimmer removes the command classes (`Hello` in HelloWorld, and the commands of DevOpsTool) because no code references them, so discovery finds no command. The two messages differ because HelloWorld does not use `CmdStarterBuilder`, which sets `Assemblies`.

## Conclusion of the first evaluation

When these measures were taken, version 2.0 did not support trimming. The library was not annotated with `RequiresUnreferencedCode` or `RequiresDynamicCode`, and `IsTrimmable` and `IsAotCompatible` were not set in the project files of `CmdStarter.Lib` or `CmdStarter.Hosting`.

- A trimmed application did not find its commands, as shown above.
- Existing applications were not affected: neither `PublishTrimmed` nor `PublishAot` is enabled by default.
- The route was a source generator that replaces discovery and binding with generated code. Its first step, the [command catalog](source-generator.md), replaces the search of the commands in an assembly that has one; its second step, the [symbol factories](source-generator.md#symbol-factories), targets the warnings `IL3050` and `IL2060` of the table above. The second evaluation below measures them.
- Workarounds, such as a root file or `DynamicDependency` attributes, were not tried, and none is supported.

The decision of that time is recorded in [ADR 0025](../adr/0025-trimming-and-native-aot.md), which the second evaluation supersedes.

## Second evaluation

This evaluation was made on 2026-11-04, after the command catalog and the typed factories of symbols were added, with the same machine and the same method as above, automated in the script `measure-trimming.ps1` at the root of the repository. The criterion is functional parity: what an application does on CoreCLR, it must do trimmed. The two samples leave features unexercised, so the script writes two more applications: `Lists`, with a `List<string>` option, an `int[]` option and a `string[]` argument, a service provider on the `Starter` with commands that it does not have registered (so they are created by `ActivatorUtilities`), a filter registered by type, a command that declares its own static `GetInstance`, and a method command with an `[Option] List<string>` parameter; and `Tree`, which places its command under a group with `Starter.UseTree`. The decision is recorded in [ADR 0029](../adr/0029-trimming-and-native-aot-second-evaluation.md).

### Static analysis

The same command as above, counting distinct warnings (code, file and line):

| Code | First evaluation | Before the annotations | Now |
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

Each warning was handled with an annotation of the member that reads the type, or with a suppression whose justification names the command catalog; the list is in the ADR. `IsTrimmable` and `IsAotCompatible` are now set in the project files of `CmdStarter.Lib` and `CmdStarter.Hosting`, and `dotnet build CmdStarter.sln` gives no warning with `TreatWarningsAsErrors`. The assembly `CmdStarter.Lib.dll` of the package carries the metadata `IsTrimmable`, read by reflection on the restored package.

### Trimmed applications

The package built with the two flags was restored in a private packages folder of the script; its SHA-256 is `E232233FB4AEBA22CAEA1E53853C7FC0C0C2DD69FD80CC5FC4C9A8C944C5707D`, the same as the package of `artefacts/`. The four applications publish with exit code 0 and no warning, with the options `-c Release -r win-x64 --self-contained -p:PublishTrimmed=true`, and run as expected (measure of 13:30:55 to 13:32:22):

| Application | Size of the published folder | Command run | Exit code | First line of the output |
| --- | ---: | --- | ---: | --- |
| HelloWorld | 21 498 346 bytes | `HelloWorld.exe --to Bob` | 0 | `Hello, Bob!` |
| DevOpsTool | 21 616 624 bytes | `DevOpsTool.exe service deploy api --version 1.2.3` | 0 | `Deploying api 1.2.3 to dev` |
| DevOpsTool | | `DevOpsTool.exe --help` | 0 | `Description:` |
| DevOpsTool | | `DevOpsTool.exe service deploy` | 1 | `Description:`, and "Argument obligatoire manquant pour la commande : 'deploy'." on the error output |
| Lists | 21 591 325 bytes | `Lists.exe show a b --names x --names y --numbers 1 2` | 0 | `names=x,y values=a,b numbers=1,2` |
| Lists | | `Lists.exe greet --who Bob` | 0 | `Hello, Bob!` |
| Lists | | `Lists.exe made` | 0 | `made by GetInstance`, and `filter: no container` on the error output |
| Lists | | `Lists.exe tag red 3 --tags a --tags b` | 0 | `label=red tags=a,b count=3` |
| Lists | | `Lists.exe show c --names z --numbers 3` | 0 | `names=z values=c numbers=3`, and `filter: Hello, filter!` on the error output |
| Tree | 21 503 992 bytes | `Tree.exe tools show --label x` | 0 | `label=x` |
| Tree | | `Tree.exe tools --help` | 0 | `Description:` |

The publications no longer report `IL2104` ("Assembly 'CmdStarter.Lib' produced trim warnings."), which they reported before the annotations: the library has no analysis warning left. What the runs show:

- The collection options survive the trimmer: `--numbers 1 2` takes two tokens only when the type of the option is recognized as a list, and so does `--tags a --tags b` on the parameter of a method command.
- A command that the container does not have registered (`greet`, `show`, `tag`) is created by `ActivatorUtilities`, with its constructor parameter; the filter registered by type is created the same way, and its line on the error output (`filter: Hello, filter!`) shows that the service was injected.
- A command that declares its own static `GetInstance` is created by it (`made by GetInstance`). A command created that way runs without the service provider ([ADR 0006](../adr/0006-dependency-injection.md)), so the measured filter has a parameterless constructor too, which gives `filter: no container`.
- `Starter.UseTree` places a command under a group, and the help of the group is not empty.

### The first measure of this evaluation

The first measure, taken with the catalog and the factories and nothing else, failed on the eight runs of the two applications of the first evaluation. The commands were found, because the catalog keeps them, but both trimmed applications stopped with `FileNotFoundException` ("Could not load file or assembly 'System.Runtime, Version=8.0.0.0'") when the catalog attribute was read on an assembly. A probe application, outside the repository, read the attribute on each loaded assembly of a trimmed application and gave the name of the culprit: `System.CommandLine.resources`, the satellite assembly of System.CommandLine for the culture of the machine (French). It references `System.Runtime` 8.0.0.0, which a trimmed application does not have; the same application not trimmed read all the assemblies. The attribute is now read only on the library or on an assembly that references it, and an assembly whose attributes cannot be loaded counts as one without a catalog. A culture other than the French one of this machine was not tried.

## Conclusion

Version 2.0 supports trimming for an application that has the command catalog, which the package generates by default, with the functions that the four measured applications use. This was measured on win-x64 with HelloWorld, DevOpsTool, Lists and Tree, which publish without warning and run as expected. What it needs is on the [Source generator](source-generator.md#trimming-and-native-aot) page.

Functions that are lost when trimmed:

- A command type that no catalog declares: a catalog written by hand that omits it, or the generator turned off. `Starter.UseTree` and `Starter.CommandsTypes` on the types of the catalog are supported (`UseTree` is measured); a type outside it is not kept by anything.
- An assembly that has no catalog: it is scanned by reflection on CoreCLR, and its types are not kept.

Not measured: the other platforms, an order of the methods that differs from the order of their declarations (see [Native AOT](native-aot.md)), and a culture other than the French one of the measure machine. Workarounds, such as a root file or `DynamicDependency` attributes, were not tried.

The Native AOT results are on the [Native AOT](native-aot.md) page. The decision is recorded in [ADR 0029](../adr/0029-trimming-and-native-aot-second-evaluation.md).

## See also

- [Native AOT](native-aot.md)
- [ADR 0025, Trimming and Native AOT](../adr/0025-trimming-and-native-aot.md)
- [Performance](performance.md)
- [Analyzers](analyzers.md)
- [Discovery](../concepts/discovery.md)
- [Dependency injection](../concepts/dependency-injection.md)
- [System.CommandLine](system-command-line.md)
