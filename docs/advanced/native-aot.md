# Native AOT

This page says where CmdStarter stands with Native AOT. The reflection used by the library and the static analysis are described on the [Trimming](trimming.md) page.

## Method

The publication uses the method of the [Trimming](trimming.md) page: an application outside the repository that references the package `cints.CmdStarter` 2.0.0-preview.1, with the sources of the HelloWorld and DevOpsTool samples.

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

The [command catalog](source-generator.md) reads an assembly that has one without calling `Assembly.GetReferencedAssemblies`, but an assembly without a catalog is still scanned, and that scan makes the call. The results above were taken without catalogs, and Native AOT with them has not been tried.

## Conclusion

Version 2.0 does not support Native AOT. The test applications publish, but they stop at start-up, before any command runs. The reasons and the other results are on the [Trimming](trimming.md) page, and the decision is recorded in [ADR 0025](../adr/0025-trimming-and-native-aot.md).

## See also

- [Trimming](trimming.md)
- [ADR 0025, Trimming and Native AOT](../adr/0025-trimming-and-native-aot.md)
- [Performance](performance.md)
- [System.CommandLine](system-command-line.md)
