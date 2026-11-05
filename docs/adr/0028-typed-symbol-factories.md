# 0028. Typed Symbol Factories

Status: Accepted
Date: 2026-11-04

## Context

After the [command catalog](0027-command-catalog.md), what remains of dynamic reflection in `CmdStarter.Lib` is the construction of generic types and methods at run time. For each option property and each handler parameter, `Starter` builds `Option<T>` and `Argument<T>` with `MakeGenericType`, sets their default value and reads their value from the parse result with `MakeGenericMethod`. These are the warnings `IL3050` and `IL2060` of the table in [Trimming](../advanced/trimming.md).

The trouble is greatest for value types. Under Native AOT, a generic instantiation over a reference type uses shared code and works. An instantiation over a value type (`Option<int>`, `Argument<bool>`) exists only if the application contains it statically; otherwise the call fails at run time. A generator can write that instantiation in the application, because it sees the types that the commands use.

This is the second of three steps that replace reflection with generated code. The first is the catalog; the third measures trimming and Native AOT. Each is its own decision.

## Decision

**A typed factory of symbols per value type.** `ISymbolFactory` (namespace `CyberInternauts.CmdStarter.Catalog`) creates the option and the argument of one value type, sets their default value, reads their value from a `ParseResult`, and gives the default value of the type. `Starter` asks for the factory of a type wherever it used to build a generic type: the creation of the options and arguments, their implicit default value, the default value of a method option, the argument of a handler without a name, and the reading of the values at execution. The attributes (description, aliases, validation, auto-completion, `[Required]`, `[Hidden]`) are still read by reflection on the members, which the catalog keeps.

**The generator registers the factories, in the catalog.** `ICommandCatalogBuilder` gains `AddSymbolType<T>()`. The generated `Describe` has one line for each distinct value type that the commands use, after the lines of the three other families and in the ordinal order of the full names. The registration over-approximates on purpose: a type registered for nothing costs one instantiation. The generator registers:

- the type of every public instance property that has a getter and a setter (a private setter and `init` included), in each listed command class and each listed global options container, with the inherited properties; the filters `[Option]`, `[NotOption]` and `[AllOptionsExcluded]` are not repeated;
- the type of every parameter of the methods marked `[Command]`, but `CancellationToken` and `CommandConsole`, which never become arguments, and the parameters passed by reference;
- the type of every parameter of the delegate that `HandlingMethod` gives, when it can be read at compile time (below).

**`HandlingMethod` is read by a rule, not by a list of forms.** The generator reads an overridden property whose body is an expression, a `get =>`, or a `get` made of a single `return`. If the static type of the expression is a concrete delegate type (`TypeKind.Delegate`, so neither `Delegate` nor `MulticastDelegate`), the parameters are those of its `Invoke`; a lambda, a cast, the creation of a delegate, a field, a property and a call all fall under this rule, and a lambda has a natural type. If it is not, and the expression is a name that designates a method group with a single method, the parameters are those of that method. Otherwise, the generator reports `CMDS0015` (severity `Info`, category Usage) on the property and registers nothing for that command. An initial list of three forms (conversion, method group, creation) had left out the most common one, the lambda, and caused 28 unjustified reports in the solution; the rule replaced it. A `HandlingMethod` that is not overridden is not reported: it is the empty delegate of the library. `CMDS0015` is `Info` and not `Warning`: under `TreatWarningsAsErrors`, a warning would break the build of a valid application.

**The delegate that runs may differ from its static type.** An `Action<object>` in an `Action<string>` field is a variant, and the binding reads the parameters of the target method. The types registered are then those of the static type, which is harmless: variance exists only between reference types, and a reference type uses the shared code.

**Types that cannot be written are left to the fallback (E1).** E1 was silent; CMDS0016 names the lost value types ([ADR 0029](0029-trimming-and-native-aot-second-evaluation.md)). The generator does not register a type that cannot be a type argument (ref-like types, pointers, function pointers, `dynamic`, type parameters, unknown types) or that the generated code cannot name: a type or an enclosing type that is `private`, `protected` or `private protected`, a `file` type, a type marked `[Obsolete(..., true)]` or `[Experimental]`, a type marked `[Obsolete]` with a `DiagnosticId` that cannot be written in a `#pragma`, and a generic type with a type argument in this case. This is the rule of the catalog for the types it leaves out ([ADR 0027](0027-command-catalog.md)), without the report at the time: nothing is lost on CoreCLR, because the fallback by reflection covers these types. Under Native AOT, such a value type is not supported; CMDS0016 now reports it (a reference type and the ref-like types and pointers are not reported). The other `[Obsolete]` types are covered by the `#pragma` of the catalog, which also lists their diagnostic ids. An array is registered by its array type.

**The fallback by reflection is the same code.** When no factory is registered for a type, the library makes the factory by reflection, once, and keeps it. It behaves like a registered one: the same types of symbols, the same default value, the same value read after a default was set (checked for `int`, `string`, `bool`, `int?`, `string[]` and `FileInfo`, by behaviour and not by reference). The library therefore works without the generator, and for a type that the generator did not see.

**The fallback never blocks a registration (E2).** The library keeps two dictionaries, one for the registered factories and one for the fallbacks. The first registration of a type wins and a second is ignored; a registration replaces a fallback that already exists, so a type used before its catalog was read still gets its registered factory afterwards.

**The default value of a value type does not change (E3).** The default value of a value type is the one `Activator.CreateInstance` gives, so a struct with an explicit parameterless constructor keeps it. It replaces the former `Activator.CreateInstance` at the three places where `Starter` needed a default: the implicit default of an option, the value of a method option, and the handler parameter without a name. A reference type has `null`, and a nullable value type too. The other `Activator.CreateInstance` calls create instances of commands and are not concerned.

**The factories are static, per process.** They replace the two caches of getters that the binding kept, which were per process too.

**`InternalsVisibleTo("CmdStarter.Tests")` on the library.** The only observable proof that a generated factory is used, and not the fallback, is in the internal registry. The attribute serves that proof alone; the other tests use the public API.

## Exclusions

The following are outside this decision:

- `DynamicInvoke` of the handler, `MethodInfo.Invoke` of the `[Command]` methods, `Activator` and `ActivatorUtilities`, and the search of `GetInstance` (generic over a reference type, hence shared code): the third step proves that they work under Native AOT with the metadata kept, and a correction comes back here if the proof fails;
- the annotations `DynamicallyAccessedMembers` and the suppressions of the library, and any measure of trimming or Native AOT;
- `CmdStarter.Hosting`.

## Consequences

- **Nothing changes on CoreCLR.** The types of the symbols, their default values and the values read are the same as before, with or without the generator. What changes is what exists statically in the application, which the next step measures.
- **Only two uses of `MakeGenericType` or `MakeGenericMethod` remain in the library**: the fallback by reflection, and the search of `GetInstance`, which is generic over a reference type.
- **A reported `CMDS0015` costs nothing at run time.** The types of the handler of that command are created by the fallback, with the same behaviour. A value type that is used only in such a handler, or that cannot be written, is not supported under Native AOT.
- **The public API grows by two members**: `ISymbolFactory` and `ICommandCatalogBuilder.AddSymbolType`. A catalog written by hand can register a type with it.
- **The generator is bigger.** It reads one property per command and walks the hierarchy of each class, at build time; at start, the catalog registers one factory per listed type.
