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

## Symbol factories

Each option and each argument is a System.CommandLine symbol typed by its value, and reading its value goes through a generic method of the parse result. Without the generator, `Starter` builds these generic types at run time, with the type of the value. A **typed factory of symbols** does it for one value type without building a generic type at run time. `ISymbolFactory` is its interface:

<!-- snippet: CmdStarter.Lib/Catalog/ISymbolFactory.cs -->
```csharp
Option CreateOption(string name);
Argument CreateArgument(string name);
void SetDefault(Option optionSymbol, object? value);
object? GetValue(ParseResult parseResult, Option optionSymbol);
```

The generator writes, in `Describe`, one line `AddSymbolType` for each distinct value type that the commands of the assembly use, after the lines of the other families and in the ordinal order of the full names. When `Starter` reads the catalog, each of these lines registers the factory of its type, and the registration lasts for the whole process. A hand-written catalog can do the same with `ICommandCatalogBuilder.AddSymbolType`.

### What is registered

The registration over-approximates on purpose: a type registered for nothing costs one instantiation.

- The type of every public instance property that has a getter and a setter (a private setter and `init` count; a property without setter, a static property and an indexer do not), in each listed command and each listed global options container, with the inherited properties. The properties that the library itself declares are not counted.
- The type of every parameter of the methods marked `[Command]`, except `CancellationToken`, `CommandConsole`, and the parameters passed by reference. A method that is not marked does not count.
- The types of the parameters of the delegate that the `HandlingMethod` of a command gives, when they can be read at compile time.

### How `HandlingMethod` is read

The generator reads the property when it is overridden in the compilation, and its body is an expression, a `get =>` or a `get` made of a single `return`. The rule is then:

1. When the static type of the expression is a concrete delegate type, the parameters are those of its `Invoke`. The expression can be a lambda (it has a natural type), a cast, the creation of a delegate, a field, a property or a call, as long as its type is a delegate type. A lambda without parameter registers nothing and reports nothing, and a `ref` parameter is not registered.
2. Otherwise, when the expression is a name that designates a method group with a single method, the parameters are those of that method.
3. Otherwise, the rule `CMDS0015` is reported (severity `Info`) on the property, and no type is registered for that command. It happens, for example, when the static type is `Delegate` itself (a field or a call of that type, a cast to `Delegate`), or when the property is not written as an expression or a getter with a single `return` (an initializer, two statements, no getter). The message says that the argument types of the handler are created by reflection at run time. The rest of the catalog is not affected. A `HandlingMethod` that is not overridden is not reported: it is the empty delegate of the library.

The first class of the hierarchy that is in the compilation and declares the property decides, and a class that implements `IStarterCommand` itself declares it too.

### The fallback by reflection

A type that is not registered is not an error. When no factory is registered for it, the library makes the same factory by reflection, once per type, and uses it. A registration made later replaces a fallback that already exists, and the first registration of a type is kept. The two factories behave the same: the types of the symbols, the default value, and the value read from a parse result are identical, checked for `int`, `string`, `bool`, `int?`, `string[]` and `FileInfo`.

The generator does not register the types that it cannot write as a type argument or that the generated code cannot name. They are:

- the types that cannot be a type argument: ref-like types such as `Span<char>`, pointers, function pointers, `dynamic`, type parameters, and unknown types;
- the types that cannot be reached from the catalog: a type, or an enclosing type, that is `private`, `protected` or `private protected`, and a `file` type;
- the types marked `[Obsolete(..., true)]` or `[Experimental]`;
- the types marked `[Obsolete]` with a `DiagnosticId` that cannot be written in a `#pragma`.

A generic type is left out when one of its type arguments is. An array is registered by its array type, such as `int[]`. For all of them the fallback by reflection is used.

On CoreCLR, the fallback loses nothing. In a Native AOT executable it does not exist for a value type, so the option or argument of a value type that the catalog cannot name is lost there. The generator reports each such value type once, with `CMDS0016` (severity `Info`), on the first member that uses it: for example `The value type 'global::Ns.Host.Mode' used by 'global::Ns.Host.HandlingMethod' cannot be registered in the command catalog ('Mode' is private and cannot be named from the catalog): under Native AOT its option or argument is not supported. Make the type reachable from a top-level class.` A nullable value type, such as `Mode?`, is a value type and is reported. Nothing is reported for a reference type, such as a `List` of such a type or an array, which shares its code, nor for ref-like types and pointers, which are not the type of an option on any platform.

### Nothing changes on CoreCLR

The factories exist so that an application can build its symbols without a generic type made at run time (the warnings `IL3050` and `IL2060`, see [Trimming](trimming.md)). On CoreCLR the symbols are the same as before. The default value of a value type is the one `Activator.CreateInstance` gives, so a struct with an explicit parameterless constructor keeps it. The whole test suite runs through the factories that the generator wrote for the test commands.

## Start-up

[Performance](performance.md) gives two series of measures of the discovery with and without catalogs, taken on 2026-11-04 (means, in μs):

| Measure | Catalog, first series | Scan, first series | Catalog, second series | Scan, second series |
| --- | --- | --- | --- | --- |
| `Assemblies` reduced to the assembly of the measures | 538 | 702 | 602 | 681 |
| every loaded assembly | 2,065 | 1,837 | 2,033 | 1,844 |

In each series, the error intervals of the two variants overlap: no measurable gain. The cold start, `FirstInstantiate`, was 148.3 ms and 147.5 ms, against 137.3 ms before catalogs; the intervals barely overlap and the measure has no twin by scan, so a cold start a dozen milliseconds slower is neither established nor excluded (see [Performance](performance.md)); settled in [ADR 0029](../adr/0029-trimming-and-native-aot-second-evaluation.md): no measurable difference.

Every searched assembly has its catalog attribute read. Without `Assemblies`, every loaded assembly pays that read, and only an assembly that has a catalog avoids its scan: the assemblies of the framework and the others are scanned as before, `GetReferencedAssemblies` included.

## Trimming and Native AOT

With the catalog and the factories, version 2.0 supports trimming and Native AOT for an application on win-x64: [Trimming](trimming.md) and [Native AOT](native-aot.md) give the measures, and [ADR 0029](../adr/0029-trimming-and-native-aot-second-evaluation.md) the decision. What an application needs:

- **The package and the generator.** The package `cints.CmdStarter` carries the generator, and it is active by default. An application that declares `CommandCatalogAttribute` itself replaces the generated catalog: it then has to declare in its catalog everything that the application uses, the commands, the classes of method commands, the global options containers and the value types.
- **Catalogs on.** `Starter.UseCommandCatalogs` stays `true`. In a runtime that does not support dynamic code, as a Native AOT executable, only the catalogs are read: no assembly is scanned, so an assembly without a catalog gives no command, and setting `Starter.UseCommandCatalogs` to `false` there makes the search throw a `CmdStarterException`.
- **A readable `HandlingMethod`.** The generator reads the delegate by the rule of [Symbol factories](#symbol-factories). When it cannot, it reports `CMDS0015`, and a value type that only that handler uses is not registered: its factory is made by reflection, which Native AOT does not support for a value type.
- **Value types that the catalog can name.** A value type that is `private`, `file`-local, `[Experimental]` or otherwise unreachable from a top-level class is not registered, and its option or argument is lost in a Native AOT executable. The generator reports it with `CMDS0016`: make the type reachable.
- **Commands from a catalog.** With the generator active, the catalog declares every command class of the assembly, so `Starter.UseTree` and `Starter.CommandsTypes` on those types are supported (both are measured). A command type that no catalog declares, because a catalog written by hand omits it or the generator is turned off, is not kept by the trimmer: that function is lost.

The class and method commands, the options and arguments (lists included), the commands created by a container that does not have them registered, the filters registered by type, the commands that declare their own static `GetInstance`, and `Starter.UseTree` were measured, trimmed and as a native executable, in [Trimming](trimming.md#second-evaluation) and [Native AOT](native-aot.md#second-evaluation). What was not measured: a culture other than the French one of the measure machine, an order of methods that differs from the order of their declarations, every platform but win-x64, and `CmdStarter.Hosting`, which is flagged by static analysis only.

## What it does not do yet

- It does not generate the binding: the options, arguments, values and instances are still read and set by reflection. The factories only replace the construction of generic types for the symbols.
- A `Starter` with no `Assemblies` still scans the assemblies that have no catalog, those of the framework included, when the runtime supports dynamic code. In a trimmed application, the scan of an assembly without a catalog is not supported.

## See also

- Examples: [HandWrittenCatalog](../../CmdStarter.Tests.CatalogCommands/HandWrittenCatalog.cs).
- Neighbouring pages: [Discovery](../concepts/discovery.md), [Analyzers](analyzers.md), [Trimming](trimming.md), [Native AOT](native-aot.md), [Performance](performance.md).
- ADRs: [0027, command catalog](../adr/0027-command-catalog.md), [0028, typed symbol factories](../adr/0028-typed-symbol-factories.md), [0029, trimming and Native AOT, second evaluation](../adr/0029-trimming-and-native-aot-second-evaluation.md).
