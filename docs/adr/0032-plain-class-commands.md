# 0032. Plain Class Commands

Status: Accepted
Date: 2026-11-05

## Context
A command of the class model must derive from `StarterCommand` or implement `IStarterCommand`, and a command of the method model is a method. A user asked why `[Command]` could not mark a class that declares no `[Command]` method, so that the class becomes a command itself. The 0.9 betas had `GenericStarterCommand<T>`, which wrapped a class that had no base class to give it a node of the tree; the 1.0 rewrite removed it because `CommandRegistration` ([ADR 0006](0006-dependency-injection.md)) now links a type, its instance and its System.CommandLine `Command`, so nothing needs to wrap the class any more.

## Decision
A **plain command** is a non-abstract class that has a namespace, is not an open generic type, does not implement `IStarterCommand`, carries `[Command]` and declares no `[Command]` method. It is a command of the class model: the discovery, the tree (`[Parent]`, `[Children]`, `ClassesBuildingMode`, `GroupsByNamespace`), `UseTree`, `CommandsTypes`, the rooting of a lonely command, the states, `[Hidden]`, `[Alias]`, `[Description]`, the validation attributes, the default values read from the description instance, the filters, `CommandHandled` and `OnCommandExecuted` apply to it as they do to a class that implements `IStarterCommand`. A class that implements `IStarterCommand` and carries `[Command]` stays an error ("A class cannot use both command models"), and so does `Handler` on a group of command methods or on a method.

**The handler** is designated in one of two ways.
- By convention, it is the only public instance method that the class declares itself. Accessors and operators, what the compiler generates, the redefinitions of the methods of `object` and a parameterless `Dispose` or `DisposeAsync` of a disposable class do not count. No method, or more than one, is an `InvalidAttributeException` when the tree is built.
- By name, with `[Command(Handler = nameof(Run))]`: the public instance method of that name, declared by the class or inherited (not from `object`). Then the class may have other public methods. A name that matches no such method, or several, is an `InvalidAttributeException`; a static method or a non-public one does not match.

In both cases the method must not be generic and must not have a `ref`, `out` or `in` parameter, as for a method command.

**Options.** Every public read-write property of the class, the ones of its base classes included, is an option, with the rules of `[Option]`, `[NotOption]` and `[AllOptionsExcluded]`. The initial value of a property is the default value of its option.

**Parameters.** The parameters of the handler follow the classification of the method model: `CancellationToken`, `CommandConsole`, a global options container, `[FromServices]`, `[Option]`, a service that `ServiceProvider` knows, and else a positional argument. A plain command has no `GetGlobalOptions<T>()`, so the container comes as a parameter. A global options container that the filters exclude is an `InvalidAttributeException`.

**Name.** `[Command("name")]` gives it, as a single segment: a name that contains a space is an `InvalidAttributeException`, because a path belongs to the method model, and a blank name is one too. Without a name, it is the kebab case of the class name. `WithName` of `UseTree` has precedence.

**Creation** follows [ADR 0006](0006-dependency-injection.md) without its first rule: `ServiceProvider` (one scope per execution), else the factory of `SetFactory`, else the parameterless constructor. The factory must return an instance of the type it was asked for, otherwise the use throws an `InvalidOperationException`. There is no `GetInstance` convention, since the class has no interface to declare one. The handler is called with `MethodInfo.Invoke`, as a method command is, and its result becomes the exit code in the same way (`int`, `Task`, `Task<int>`, `ValueTask`, `void`).

**Discovery.** The scan and the catalog look for a plain command among the classes of the assemblies that reference the library and among the commands that a catalog lists, never among all the types of all the assemblies. `CommandsTypes` and `UseTree` accept it.

## Consequences
- `Starter.SetFactory` takes a `Func<Type, object>` and `Starter.FindCommandInstance(Command)` returns `object?`: a lambda that returns an `IStarterCommand` still assigns, and a caller that used the result as an `IStarterCommand` casts it. `ICommandCatalogBuilder.AddCommand<T>` only needs a class. `Starter.FindCommand<T>`, `Starter.GetFullCommandString<T>`, `CommandTreeDefinition.Add<T>` and `[Parent<T>]` have no constraint at all: they use only `typeof(T)`, and generic code that constrains its parameter to `IStarterCommand` keeps compiling. `CommandAttribute` has a `Handler` property.
- `FindCommandInstance<T>` and `CommandContext.Instance` give the plain instance. `CommandRegistration` carries the method and the parameter bindings of a plain command.
- The command catalog lists a plain command with `AddCommand<T>`, and the analyzer and the generator know the definition (see [ADR 0018](0018-roslyn-analyzers.md) and [ADR 0027](0027-command-catalog.md)).
- What does not apply: `GetInstance`, a path in the name, `GetGlobalOptions<T>()`, `HandlingMethod` and the other members of `IStarterCommand` (the class has none of them).
