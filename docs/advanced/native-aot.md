# Native AOT

This page says where CmdStarter stands with Native AOT. The reflection used by the library and the static analysis are described on the [Trimming](trimming.md) page. It holds two evaluations, both of 2026-11-04. The first was made before the command catalog and the typed factories of symbols existed, and found that Native AOT did not work. The [second evaluation](#second-evaluation) was made after them and supersedes it: version 1.0 supports Native AOT for an application that has the command catalog. On 2026-11-05 it was measured again with [the package that nuget.org serves](#the-package-that-nugetorg-serves).

## Method

The publication uses the method of the [Trimming](trimming.md) page: an application outside the repository that references the package `cints.CmdStarter` built in `artefacts/`, with the sources of the HelloWorld and DevOpsTool samples.

```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishAot=true
```

## Native AOT applications

Native AOT needs the native toolchain of Visual Studio, the C++ build tools. On the test machine the toolchain was installed, but the first publication failed, with exit code 1 (the machine's tools write their messages in French):

```text
…\microsoft.dotnet.ilcompiler\10.0.14\build\Microsoft.NETCore.Native.targets(396,5): error MSB3073: La commande ""'vswhere.exe' n'est pas reconnu en tant que commande interne ou externe, un programme exécutable ou un fichier de commandes.;C:\Program Files\Microsoft Visual Studio\18\Community\VC\Tools\MSVC\14.51.36231\bin\Hostx64\x64\link.exe" @"obj\Release\net10.0\win-x64\native\link.rsp"" s'est arrêtée avec le code 123.
```

The file `vswhere.exe` exists in the Visual Studio installer folder, `C:\Program Files (x86)\Microsoft Visual Studio\Installer`, but that folder was not on the PATH. This is a problem of the machine, not of the library. Adding the folder to the PATH of the publishing process only made the publication succeed. It gave two warnings for each application, `IL2104` and `IL3053` ("Assembly 'CmdStarter.Lib' produced AOT analysis warnings."), and exit code 0.

The published executables fail when they run:

| Application | Size of the native executable | Command run | Exit code |
| --- | ---: | --- | ---: |
| HelloWorld | 4 984 832 bytes | `HelloWorld.exe --to Bob` | `-1073740791` (`0xC0000409`) |
| DevOpsTool | 5 217 792 bytes | `DevOpsTool.exe service deploy api --version 1.2.3` | `-1073740791` |

The output of HelloWorld begins with:

```text
Unhandled exception. System.PlatformNotSupportedException: Operation is not supported on this platform.
   at System.Reflection.Runtime.Assemblies.RuntimeAssemblyInfo.GetReferencedAssemblies()
   at CyberInternauts.CmdStarter.Starter.<>c__DisplayClass115_0.<FindCommandsTypes>b__1(Assembly assembly)
```

The stack goes on through `Enumerable`, `Starter.FindCommandsTypes()` and `BuildTree()`. DevOpsTool gives the same exception and the same first frame, and its stack goes through `CmdStarterServiceCollectionExtensions.AddCmdStarter`.

The executable stops before any command runs. The native runtime does not support `Assembly.GetReferencedAssemblies`, which the discovery code calls. The method-command path goes through the same call.

An application that describes its tree with `Starter.UseTree` was not tried.

The [command catalog](source-generator.md) reads an assembly that has one without calling `Assembly.GetReferencedAssemblies`, but at that time an assembly without a catalog was still scanned, and that scan made the call. The results above were taken without catalogs; the second evaluation below tried them.

## Conclusion of the first evaluation

When these measures were taken, the library did not support Native AOT. The test applications published, but they stopped at start-up, before any command ran. The reasons and the other results are on the [Trimming](trimming.md) page, and the decision of that time is recorded in [ADR 0025](../adr/0025-trimming-and-native-aot.md), which the second evaluation supersedes.

## Second evaluation

This evaluation was made on 2026-11-04, after the command catalog and the typed factories of symbols were added, with the same machine, the same toolchain and the same method, automated in the script `measure-trimming.ps1` at the root of the repository (see [Trimming](trimming.md#second-evaluation)). The decision is recorded in [ADR 0029](../adr/0029-trimming-and-native-aot-second-evaluation.md).

### The first measure of this evaluation

Taken with the catalog and the factories and nothing else, it published the applications and failed on the Native AOT runs, for two reasons:

- HelloWorld stopped with `System.PlatformNotSupportedException` ("Operation is not supported on this platform.") raised by `Assembly.GetReferencedAssemblies()` in the search of the commands. The application creates `new Starter()` without `Assemblies`, so the search reached the assemblies of the framework, which have no catalog, and scanned them.
- DevOpsTool stopped with `System.InvalidOperationException` ("There is no metadata token available for the given member.") in the code that adds the method commands of a class, which sorted the methods by their metadata token to keep the order of their declarations. A Native AOT executable has no metadata token.

Without dynamic code, which is the case in a Native AOT executable, the search now reads only the catalogs: no assembly is scanned and `Assembly.GetReferencedAssemblies` is never called. The methods of a class keep the order that `GetMethods` gives. With `Starter.UseCommandCatalogs` set to `false`, the search throws a `CmdStarterException` that says that catalogs must be on, because nothing could be read. These two changes are tested with assemblies made for the tests that throw `PlatformNotSupportedException` when their references are asked.

### Native AOT applications

The static analysis gives no `IL` warning (see [Trimming](trimming.md#static-analysis)), and the package carries the metadata `IsAotCompatible`. The criterion is functional parity: what an application does on CoreCLR, it must do as a native executable. Four applications are measured: HelloWorld and DevOpsTool, and two that the script writes ([Trimming](trimming.md#second-evaluation) describes them): `Lists`, with collection options, a service provider with commands that it does not have registered, a filter registered by type, a command that declares its own static `GetInstance` and a method command with an `[Option] List<string>` parameter; and `Tree`, which places its command under a group with `Starter.UseTree`, or sets `Starter.CommandsTypes` to a type of the catalog. They publish with `-c Release -r win-x64 --self-contained -p:PublishAot=true`, exit code 0 and no warning (the publications reported `IL2104` and `IL3053` before the annotations). The package restored has the same SHA-256 as the package built, `30500178874EA7430641CAD873CA8028EBA18E79C9BD020693E0B04E2A6FD1EB`. They run as expected (measure of 22:44:08 to 22:46:15):

| Application | Size of the native executable | Command run | Exit code | First line of the output |
| --- | ---: | --- | ---: | --- |
| HelloWorld | 4 996 096 bytes | `HelloWorld.exe --to Bob` | 0 | `Hello, Bob!` |
| DevOpsTool | 5 243 904 bytes | `DevOpsTool.exe service deploy api --version 1.2.3` | 0 | `Deploying api 1.2.3 to dev` |
| DevOpsTool | | `DevOpsTool.exe --help` | 0 | `Description:` |
| DevOpsTool | | `DevOpsTool.exe service deploy` | 1 | `Description:`, and "Argument obligatoire manquant pour la commande : 'deploy'." on the error output |
| Lists | 5 204 480 bytes | `Lists.exe show a b --names x --names y --numbers 1 2` | 0 | `names=x,y values=a,b numbers=1,2` |
| Lists | | `Lists.exe greet --who Bob` | 0 | `Hello, Bob!` |
| Lists | | `Lists.exe made` | 0 | `made by GetInstance`, and `filter: Hello, filter!` on the error output |
| Lists | | `Lists.exe tag red 3 --tags a --tags b` | 0 | `label=red tags=a,b count=3` |
| Lists | | `Lists.exe show c --names z --numbers 3` | 0 | `names=z values=c numbers=3`, and `filter: Hello, filter!` on the error output |
| Lists | | `Lists.exe reporter hello --prefix note --level 5` | 0 | `note: hello level=5`, and `filter: Hello, filter!` on the error output |
| Tree | 5 032 960 bytes | `Tree.exe tools show --label x` | 0 | `label=x` |
| Tree | | `Tree.exe tools --help` | 0 | `Description:` |
| Tree | | `Tree.exe ct show --label y` | 0 | `label=y` |

What these runs go through under Native AOT:

- The creation of a command with its parameterless constructor (`Activator.CreateInstance`) and the invocation of a handler (`Delegate.DynamicInvoke`) in HelloWorld and Tree.
- The invocation of the methods marked `[Command]` (`MethodInfo.Invoke`), in DevOpsTool and in `Lists` (`tag`, whose `[Option]` parameter is a list and whose other parameter is an argument).
- The creation of a class that has a constructor parameter by the Microsoft dependency injection container, which has the class registered (DevOpsTool); and by `ActivatorUtilities.CreateInstance` when the container does not have it registered: the commands `greet`, `show` and `tag` of `Lists`, and the filter registered by type, whose line on the error output (`filter: Hello, filter!`) shows that the service was injected.
- The creation of a command by its own static `GetInstance` (`made`): the search of the method, `MakeGenericMethod` on a class, then `Invoke`. The execution of a command created that way still has the scope of the service provider ([ADR 0006](../adr/0006-dependency-injection.md), amended on 2026-11-04), so its filter is created with its services, which gives `filter: Hello, filter!`.
- The read of the catalog attribute and of the loaded assemblies (`AppDomain.GetAssemblies`).
- `Starter.UseTree` with a group, and `Starter.CommandsTypes` set to a type of the catalog (`Tree`).
- In System.CommandLine, the parsing, the help, a parse error and its exit code, the options with default values, and the binding of arguments and options.

The help and the results of eight more commands of DevOpsTool, run by hand, were identical in the trimmed and the native executables.

In DevOpsTool, `service --help` lists `deploy` then `rollback` in the Native AOT executable, as in the trimmed one. The order of the methods without dynamic code is the one that `GetMethods` gives; it was not tried with a class whose methods are not in the order of their declarations.

### The package that nuget.org serves

On 2026-11-05, after the publication of version 1.0.0, the four applications were published again as native executables with the package `cints.CmdStarter` 1.0.0 that nuget.org serves, restored from nuget.org only (`measure-trimming.ps1 -FromNuGet`, measure of 19:08:24 to 19:10:23). That package carries the repository signature of nuget.org; its SHA-256 is `57453BED1217606E44CBFB4423E6A01F0511EDB978496D5E5A5525E7C7B74CCC`, the same as the restored package, and apart from the signature file its entries are those of the package built in `artefacts/` (see [Trimming](trimming.md#the-package-that-nugetorg-serves)). The four applications publish with `-c Release -r win-x64 --self-contained -p:PublishAot=true`, exit code 0 and no warning, and the thirteen commands of the table above give the same exit codes and the same first lines:

| Application | Size of the native executable |
| --- | ---: |
| HelloWorld | 5 008 896 bytes |
| DevOpsTool | 5 256 192 bytes |
| Lists | 5 219 328 bytes |
| Tree | 5 045 248 bytes |

## Conclusion

Version 1.0 supports Native AOT for an application that has the command catalog, which the package generates by default, with the functions that the four measured applications use. This was measured on win-x64 with HelloWorld, DevOpsTool, Lists and Tree, with the package built from the sources and with the package that nuget.org serves. What it needs is on the [Source generator](source-generator.md#trimming-and-native-aot) page.

Functions that are lost in a Native AOT executable:

- An assembly without a catalog is not scanned, so its commands are not found; with `Starter.UseCommandCatalogs` set to `false`, the search throws a `CmdStarterException`. A command type that no catalog declares (a catalog written by hand that omits it, or the generator turned off) is lost too; `Starter.UseTree` and `Starter.CommandsTypes` on the types of the catalog are supported (both are measured).
- A value type that the generator cannot register, or that is used only in a handler whose delegate it cannot read, uses the fallback by reflection, which is not supported in a Native AOT executable: an option or an argument of that type is lost. The generator reports each such value type once at compile time, with the diagnostic `CMDS0016` (see [Analyzers](analyzers.md)).

Not measured: the other platforms.

The trimming results are on the [Trimming](trimming.md) page.

## See also

- [Trimming](trimming.md)
- [ADR 0025, Trimming and Native AOT](../adr/0025-trimming-and-native-aot.md)
- [Performance](performance.md)
- [System.CommandLine](system-command-line.md)
