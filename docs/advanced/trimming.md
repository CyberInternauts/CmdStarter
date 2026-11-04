# Trimming

This page says where CmdStarter stands with trimming.

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

## Conclusion

Version 2.0 does not support trimming. The library is not annotated with `RequiresUnreferencedCode` or `RequiresDynamicCode`, and `IsTrimmable` and `IsAotCompatible` are not set in the project files of `CmdStarter.Lib` or `CmdStarter.Hosting`.

- A trimmed application does not find its commands, as shown above.
- Existing applications are not affected: neither `PublishTrimmed` nor `PublishAot` is enabled by default.
- A source generator, which would replace discovery and binding with generated code, is the route under consideration. Its first step exists: the [command catalog](source-generator.md) replaces the search of the commands in an assembly that has one. The results on this page were taken without it, and trimming with the catalog has not been measured. The second step, the [symbol factories](source-generator.md#symbol-factories), targets the warnings `IL3050` and `IL2060` of the table above and will be measured in a later step; the results of this page do not change.
- Workarounds, such as a root file or `DynamicDependency` attributes, were not tried, and none is supported.

The decision is recorded in [ADR 0025](../adr/0025-trimming-and-native-aot.md). The Native AOT results are on the [Native AOT](native-aot.md) page.

## See also

- [Native AOT](native-aot.md)
- [ADR 0025, Trimming and Native AOT](../adr/0025-trimming-and-native-aot.md)
- [Performance](performance.md)
- [Analyzers](analyzers.md)
- [Discovery](../concepts/discovery.md)
- [Dependency injection](../concepts/dependency-injection.md)
- [System.CommandLine](system-command-line.md)
