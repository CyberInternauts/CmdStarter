# 0009. Method-Based Command Model

Status: Accepted
Date: 2026-11-03

## Context
The class-based command model maps each `IStarterCommand` class to a command, its properties to options, and its `HandlingMethod` parameters to arguments. Applications also need a direct way to expose ordinary methods as commands and receive services through method parameters.

## Decision
`[Command]` can mark a class or a method, and the class need not implement `IStarterCommand`.
A marked class is a root-level group; its marked methods are child commands.
Marked methods in an unmarked class become root-level commands.
Explicit names follow the `StarterCommand` naming rule; inferred type and method names use kebab case, with a final `Async` removed from methods.
Only public methods declared on the class are commands, ordered by metadata token.
Discovery uses classes that pass the existing assembly, namespace, and class filters.
Only the library assembly and assemblies referencing it are scanned for method commands.
`CancellationToken` and global options containers are supplied by CmdStarter.
`[FromServices]` explicitly requests dependency injection; `[Option]` explicitly creates an option.
When a configured provider identifies a parameter type as a service, it is inferred as a service.
All remaining parameters become positional arguments, preserving defaults and parameter metadata.
Parameter options support required, hidden, description, alias, completion, multiple values, and defaults.
An instance method receives a new class instance on every execution; static methods need no instance.
Microsoft DI supplies method instances and services from an execution scope when available.
CmdStarter disposes instances it creates, while the scope owns instances resolved from DI.
Integer and asynchronous integer results set the exit code; other supported or ignored results return zero.
The class-based lonely-command root behavior applies only when no method commands were found.
Invalid method definitions and duplicate sibling names raise `InvalidAttributeException` during instantiation.
`NoCommandFoundException` is raised only when both command models are empty after filtering.

## Consequences
The method-based model complements the class-based model and shares its command tree, parsing, global options, and execution lifecycle.
See [ADR 0004](0004-class-based-command-model.md) for the class-based model.
