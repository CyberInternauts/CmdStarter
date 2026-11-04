# 0006. Dependency Injection

Status: Accepted
Date: 2026-11-03

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
Services resolved by a scope follow Microsoft DI disposal rules, including asynchronous-only services.
Custom factories must dispose instances they own.
Concurrent command runs require separate `Starter` instances.
