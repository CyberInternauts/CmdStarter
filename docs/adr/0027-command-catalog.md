# 0027. Command Catalog Generated at Compile Time

Status: Accepted
Date: 2026-11-04

## Context

`Starter` finds the command classes, the classes of method commands and the global options containers by scanning, with reflection, every assembly it searches: the types of the assembly (`GetLoadableTypes`) and, for method commands, the references of the assembly (`GetReferencedAssemblies`). This costs 1.62 ms at start-up in the series of [performance](../advanced/performance.md), it only sees assemblies that are loaded, and it is the first thing that fails when an application is trimmed or published with Native AOT: the trimmer removes a command class that no code references, and the native runtime does not support `GetReferencedAssemblies` ([ADR 0025](0025-trimming-and-native-aot.md), [Trimming](../advanced/trimming.md), [Native AOT](../advanced/native-aot.md)).

A catalog written at compile time lists these types, so that the search does not need the scan for an assembly that has one. This is the first of three steps that replace reflection with generated code. The next ones are the binding of options, arguments and instances, and the measure of trimming and Native AOT; each is its own decision.

## Decision

**The generator lives in `CmdStarter.Analyzers`.** It is an incremental generator of the assembly that already ships in the package `cints.CmdStarter`, under `analyzers/dotnet/cs`. There is no new package and no new dependency.

**It writes one file in every compilation that references CmdStarter.Lib.** The file is `CommandCatalog.g.cs`. It declares an internal class `CommandCatalog_<assembly name>` that implements `ICommandCatalog`, in the namespace `CyberInternauts.CmdStarter.Generated`, and the assembly attribute `CommandCatalogAttribute` that designates it. The name of the class carries the name of the assembly (a character that cannot be in an identifier becomes `_`), so that two assemblies that see each other through `InternalsVisibleTo` never see two internal types of the same name. A catalog with no type is written too: it tells the engine that there is nothing to search in the assembly. Nothing is written when the compilation does not reference the library, or references a version of it that has no catalog API.

**The catalog repeats the predicate of the scan and validates nothing.** Validation stays with the engine, which raises the same `InvalidAttributeException`.

- A command class is a non-abstract class that implements `IStarterCommand` and has a namespace.
- A class of method commands is a class with a namespace that is marked `[Command]` or declares a method marked `[Command]`, whatever the accessibility of the method. Only what the class itself declares counts, because `[Command]` is not inherited.
- A global options container is a class, abstract or not, that implements `IGlobalOptionsContainer`. No namespace is required.
- A nested type that can be reached is listed by its full name.

Amended on 2026-11-05 (lot 037): a plain command ([ADR 0032](0032-plain-class-commands.md)), a concrete class marked `[Command]` that declares no `[Command]` method and implements no `IStarterCommand`, is a command class too. The generator lists it with `AddCommand`, whose type parameter now only needs to be a class, registers the types of its read/write properties and of the parameters of its handler, and no longer lists it with `AddMethodCommands`. The engine evaluates the definition of a plain command only on the types of the assemblies that reference the library and on the types that a catalog lists as commands, never on all the types of all the assemblies.

**The public API is in `CyberInternauts.CmdStarter.Catalog`.** `CommandCatalogAttribute` designates the catalog type of an assembly. `ICommandCatalog` has one method, `Describe`, that receives an `ICommandCatalogBuilder`. The builder declares a command class with `AddCommand`, a class of method commands with `AddMethodCommands`, and a container with `AddGlobalOptions`. `AddMethodCommands` takes a `Type` and not a type argument, because most classes of method commands are static, and a static class cannot be a type argument. The generated code has one line per type, in the ordinal order of the full names. The annotations of the parameters (`DynamicallyAccessedMembers`) are the ones that will keep the members of the commands when the application is trimmed. They change nothing at run time.

**The engine reads a catalog assembly by assembly.** When an assembly carries the attribute and `Starter.UseCommandCatalogs` is `true`, `Starter` creates the catalog with its public parameterless constructor, calls `Describe`, and uses the declared types as the candidates of that assembly. It calls neither `GetLoadableTypes` nor `GetReferencedAssemblies` on it. Any other assembly is scanned as before. The filters (`Assemblies`, `Namespaces`, `Classes`), the ordinal sort, `UseTree`, `CommandsTypes` and `NoCommandFoundException` do not change. The predicate of the scan is applied to the types of a catalog as well: a type that the scan would not find is ignored, so a catalog written by hand cannot add a command that the scan would never have found, an abstract one for example. A catalog that cannot be created, or whose `Describe` throws, is a configuration error: `InvalidAttributeException` names the assembly and the type of the catalog, and gives the reason.

**`Starter.UseCommandCatalogs` is `true` by default.** Setting it resets the discovery, like `Assemblies`. When it is `false`, every assembly is scanned: this is the former behaviour, and what lets tests compare the two.

**Global options containers follow the same rule, once per process.** `GlobalOptionsManager` finds the containers of all the loaded assemblies once, and keeps them for the process. It does this when the first `Starter` builds its tree, no longer in the constructor of `Starter`: a catalog that fails then raises a configuration error when the tree is built, and `new Starter()` never throws. The setting of that first `Starter` decides how the containers are found, and a later `Starter` with another setting does not find them again.

**A catalog written by hand wins.** When the compilation already declares `CommandCatalogAttribute`, the generator writes nothing. This is how an application replaces the generated catalog, and how it turns the generator off.

**The generator is wired on the projects of the solution that have commands**: the test commands, the demos and the benchmarks. The samples and the test application already reference the analyzer package and get a catalog without any change.

## Differences from the scan

The catalog never breaks a valid compilation: the generated file switches off the obsolescence warnings (`CS0612`, `CS0618`, and the own diagnostic id of an `[Obsolete]` type), and the generator leaves out what the generated code could not name. A type that the generator leaves out is reported with `CMDS0014`, of severity `Info`, at its declaration, with the reason. The differences with the scan are:

- **Open generic type definitions** are left out (`open generic type definition`). The scan found them and then failed to create them.
- **A type that cannot be reached** from a top-level class of the assembly is left out: a type or an enclosing type that is `private`, `protected` or `private protected`, or a `file` type. A `protected internal` type stays.
- **A type nested in a generic type** is left out.
- **A type marked `[Obsolete(..., true)]`**, or one of its enclosing types, is left out, as is a type marked `[Experimental]`.
- **A type marked `[Obsolete]` with a `DiagnosticId`** is listed, and the id is added to the `#pragma warning disable` line. An id that cannot be written in a `#pragma` (it must match `[A-Za-z_][A-Za-z0-9_]*`) leaves the type out.
- **A listed type that does not load** makes the whole catalog fail, because `Describe` is compiled as one block. The scan skips only that type ([Discovery](../concepts/discovery.md)). The error is a configuration error, kept as such.
- **An assembly that is not loaded** is invisible, as it was.
- **A catalog written by hand** replaces the generated one entirely.

## Exclusions

The following are outside this decision:

- the generated binding of options, arguments, values and instances;
- the annotations for trimming and Native AOT of the library, and any measure of them;
- the scan of the assemblies that have no catalog, the assemblies of the framework included.

## Consequences

- **What the catalog is for.** It is made for trimming and Native AOT. The annotations of its parameters are meant to keep the types it declares, which a later step measures, and the search calls neither `GetReferencedAssemblies` nor `GetTypes` on an assembly that has a catalog. Those two points are what the next steps build on; this step alone does not make an application trimmable or compatible with Native AOT.
- **What it does not bring.** It is not made for speed on CoreCLR, and the series in [performance](../advanced/performance.md) measures no gain there. Every searched assembly has its catalog attribute read; without `Assemblies`, every loaded assembly pays that read, and only an assembly that has a catalog avoids its scan. A `Starter` that sets `Assemblies` to its assemblies that have a catalog avoids their scan, and nothing more. A `Starter` with no `Assemblies` still scans the assemblies of the framework, `GetReferencedAssemblies` included. Cold start: possibly a few milliseconds slower, not settled when this was written; settled in [ADR 0029](0029-trimming-and-native-aot-second-evaluation.md): no measurable difference.
- **Global options.** The static cache means that the first `Starter` of a process fixes the mode of the containers.
- **The test commands carry a `NoWarn` for the CMDS rules.** `CmdStarter.Tests.Commands` is test data that contains commands that are wrong on purpose, under `Erroneous`, `Methods/Erroneous` and `GlobalOptions/Filters`, which the CMDS rules rightly report. The project references the analyzer package for its generator, and the rules come with it. The rules stay proved by `CmdStarter.Analyzers.Tests`, `CMDS0014` is never suppressed, and the shipped code is not touched. It is the only `NoWarn` for a CMDS rule in the solution.
- **Assemblies that are not loaded** stay invisible, with or without a catalog: the catalog is read only for an assembly that the search reaches.
