# 0006. Dependency Injection

Status: Accepted
Date: 2026-11-03
Amended: 2026-11-04 (see Consequences)

## Context
Applications need per-invocation service scopes while CmdStarter should remain usable without a specific container.

## Decision

CmdStarter creates a description instance while building the command tree and a new execution instance for every command run.
Creation priority is a `GetInstance<T>()` method declared by the command type, `Starter.ServiceProvider`, `Starter.SetFactory`, then the parameterless constructor.
Microsoft DI executions use one asynchronous service scope per execution; the scope is disposed even when a handler throws.
The description instance resolved from Microsoft DI uses its own scope, disposed after the tree and global options are built.
CmdStarter disposes instances it creates itself: through a `GetInstance<T>()` method declared by the command type, `Activator` or `ActivatorUtilities`.
The DI scope disposes command instances and services resolved from that scope.
A custom factory and `IServiceManager` retain ownership of the instances they return; CmdStarter does not dispose those instances.
`CmdStarterBuilder` enables `ValidateScopes` and `ValidateOnBuild` by default.
`IServiceManager` is obsolete and is planned for removal in 3.0. Applications should set `Starter.ServiceProvider` or use `CmdStarterBuilder` with Microsoft.Extensions.DependencyInjection. For another container, use `Starter.SetFactory`.
A single `Starter` is not intended to execute concurrent `Start` calls because parsed global options are stored by that instance.

## Consequences

Applications can use Microsoft DI directly or provide a factory for another container. Existing `IServiceManager` implementations can migrate to `SetFactory` before the API is removed in 3.0.
Applications should register scoped dependencies when a command needs state isolated to one execution.
For Generic Host integration, see [ADR 0015](0015-generic-host-integration.md), including the `IServiceCollection.AddCmdStarter` registration API.
Services resolved by a scope follow Microsoft DI disposal rules, including asynchronous-only services.
Amended on 2026-11-04: the execution scope is opened as soon as `Starter.ServiceProvider` is set, whichever means creates the command instance, so a command created by its own `GetInstance<T>()` also has it. The filters registered by type and `CommandContext.Services` are served by that scope, which is disposed at the end of the execution like the others; the instance still comes from `GetInstance<T>()` and CmdStarter still disposes it. The order of creation does not change, and neither does `Starter.SetFactory`: the factory is called only when `Starter.ServiceProvider` is not set, so there is no scope for it. Before this amendment such an execution had no scope and `CommandContext.Services` was `null`, so a filter registered by type was created by its parameterless constructor, without its services; the measure of the trimming evaluation showed it ([ADR 0029](0029-trimming-and-native-aot-second-evaluation.md)).
Custom factories must dispose instances they own.
Concurrent command runs require separate `Starter` instances.
