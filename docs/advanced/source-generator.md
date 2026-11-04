# Source generator and command catalog

The package `cints.CmdStarter` ships a source generator. In each project that references CmdStarter, it writes at compile time a catalog of the commands of the project. `Starter` reads the catalog of an assembly instead of scanning its types by reflection. This page covers what the generator writes, how `Starter` reads it, what differs from the scan, and what it gives at start-up.

## What the generator writes

The generator is in the same assembly as the [analyzers](analyzers.md), so it needs no extra package. In every compilation that references CmdStarter.Lib, it writes a file `CommandCatalog.g.cs` with:

- the assembly attribute `CommandCatalogAttribute`, which designates the catalog;
- an internal class `CommandCatalog_<assembly name>` that implements `ICommandCatalog`. Its `Describe` method has one line per type: `AddCommand` for a command class, `AddMethodCommands` for a class of method commands, and `AddGlobalOptions` for a global options container, in the ordinal order of the full names.

The catalog lists the types that the scan would find: non-abstract classes that implement `IStarterCommand` and have a namespace; classes with a namespace that are marked `[Command]` or declare a method marked `[Command]`; and classes that implement `IGlobalOptionsContainer`. A project with no command gets an empty catalog, which tells `Starter` that there is nothing to search in the assembly. Nothing is written in a compilation that does not reference CmdStarter.Lib.

The catalog validates nothing. The checks stay in `Starter`, which raises the same exceptions as before.

The generated class is internal and its name carries the name of the assembly, so two assemblies that see each other with `InternalsVisibleTo` never see two types of the same name.

## How `Starter` reads it

`Starter` searches the assemblies of `Starter.Assemblies`, or all the loaded assemblies when the list is empty. For each assembly, in this order:

1. When `Starter.UseCommandCatalogs` is `true` (the default) and the assembly carries `CommandCatalogAttribute`, `Starter` creates the catalog, calls `Describe`, and takes the declared types as the candidates of the assembly. It does not load the types of the assembly and does not read its references.
2. Otherwise, it scans the assembly by reflection.

The filters, the sort and the exceptions are the same for both. The predicate of the scan is also applied to the types of a catalog: a type that the scan would not find is ignored. The `HandWrittenCatalog` of the test assembly shows it. It declares two commands, one of them abstract:

<!-- snippet: CmdStarter.Tests.CatalogCommands/HandWrittenCatalog.cs -->
```csharp
public sealed class HandWrittenCatalog : ICommandCatalog
{
    public void Describe(ICommandCatalogBuilder builder)
    {
        builder.AddCommand<Listed>();
        builder.AddCommand<AbstractListed>();
    }
}
```

The assembly also holds a command, `Unlisted`, that the catalog does not declare. With the catalog, `Starter.CommandsTypes` is the list of `Listed` alone: `Unlisted` is not read, because the scan is skipped, and the abstract command is ignored. With `UseCommandCatalogs` set to `false`, the scan finds `Listed` and `Unlisted`. Changing the setting resets the discovery.

A catalog that cannot be created (no public parameterless constructor, a constructor that throws) or whose `Describe` throws is a configuration error: `InvalidAttributeException`, whose message names the assembly and the type of the catalog and gives the reason.

## The global options containers

The containers are found once for the process, in the assemblies that are loaded, by the first `Starter` that builds its tree. That search follows the same rule, assembly by assembly, with the setting `UseCommandCatalogs` of that first `Starter`. A `Starter` that is created later and has another setting does not find them again. Building the tree is when a faulty catalog is reported, and the constructor of `Starter` never throws because of one.

## Differences from the scan

The generated code must never break a valid compilation. It switches off the warnings for obsolete types (`CS0612`, `CS0618`, and the own diagnostic id of an `[Obsolete]` type), and the generator leaves out the types it could not name. A type that is left out is reported with the rule `CMDS0014` (severity `Info`, see [Analyzers](analyzers.md)), with the reason:

| Type | Reason reported |
| --- | --- |
| a generic type definition, such as `Open<T>` | `open generic type definition` |
| a type, or an enclosing type, that is `private`, `protected` or `private protected`, or a `file` type | it cannot be named from the catalog |
| a type nested in a generic type | it is nested in the generic type |
| a type, or an enclosing type, marked `[Obsolete(..., true)]` or `[Experimental]` | it is obsolete as an error, or experimental |
| a type marked `[Obsolete]` with a `DiagnosticId` that cannot be written in a `#pragma` | the diagnostic id cannot be suppressed in the catalog |

A `protected internal` type stays in the catalog. An `[Obsolete]` type with a `DiagnosticId` that can be written is listed, and its id is added to the `#pragma warning disable` line of the generated file.

Two other differences matter:

- A listed type that does not load makes the whole catalog fail, because `Describe` is compiled as one block. The scan skips only that type.
- A catalog does not make an assembly that is not loaded visible: `Starter` searches the assemblies that are loaded, or those in `Starter.Assemblies`.

## A catalog written by hand

An application that declares `CommandCatalogAttribute` itself replaces the generated catalog: the generator writes nothing when the attribute is already there. This is also how the generator is turned off for a project. `HandWrittenCatalog` above is such a catalog, and the attribute is declared in the same assembly:

<!-- snippet: CmdStarter.Tests.CatalogCommands/HandWrittenCatalog.cs -->
```csharp
[assembly: CommandCatalog(typeof(CyberInternauts.CmdStarter.Tests.Commands.Catalog.HandWrittenCatalog))]
```

## Start-up

[Performance](performance.md) gives two series of measures of the discovery with and without catalogs, taken on 2026-11-04 (means, in μs):

| Measure | Catalog, first series | Scan, first series | Catalog, second series | Scan, second series |
| --- | --- | --- | --- | --- |
| `Assemblies` reduced to the assembly of the measures | 538 | 702 | 602 | 681 |
| every loaded assembly | 2,065 | 1,837 | 2,033 | 1,844 |

In each series, the error intervals of the two variants overlap: no measurable gain. The cold start, `FirstInstantiate`, was 148.3 ms and 147.5 ms, against 137.3 ms before catalogs; its error intervals overlap those of the earlier series as well: no measurable gain.

Every searched assembly has its catalog attribute read. Without `Assemblies`, every loaded assembly pays that read, and only an assembly that has a catalog avoids its scan: the assemblies of the framework and the others are scanned as before, `GetReferencedAssemblies` included.

## What it does not do yet

- It does not generate the binding: the options, arguments, values and instances are still read by reflection.
- It does not make trimming or Native AOT work. [Trimming](trimming.md) and [Native AOT](native-aot.md) give the results taken without it, and neither has been measured with a catalog.
- A `Starter` with no `Assemblies` still scans the assemblies that have no catalog, those of the framework included.

## See also

- Examples: [HandWrittenCatalog](../../CmdStarter.Tests.CatalogCommands/HandWrittenCatalog.cs).
- Neighbouring pages: [Discovery](../concepts/discovery.md), [Analyzers](analyzers.md), [Trimming](trimming.md), [Native AOT](native-aot.md), [Performance](performance.md).
- ADR: [0027, command catalog](../adr/0027-command-catalog.md).
