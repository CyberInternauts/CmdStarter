# 0016. Fluent Command Tree

Status: Accepted
Date: 2026-11-03

## Context

Class-based commands are placed in the command tree through `[Parent]` and `[Children]` attributes or namespace-based `ClassesBuildingMode`. Applications that want to keep that structure in composition code need an explicit, readable way to place commands without attributes or namespace conventions.

## Decision

`Starter.UseTree` accepts a callback that builds an explicit command tree through `CommandTreeDefinition`. `Add` places command classes, `Group` adds a group without a command class, `WithName` changes a command's tree name, and `WithAliases` adds command or group aliases. `CmdStarterOptions` and `CmdStarterBuilder` expose `UseTree` shortcuts for dependency injection composition.

When a definition is supplied, it replaces discovery and placement of command classes. `Assemblies`, `Namespaces`, `Classes`, `ClassesBuildingMode`, `[Parent]` and `[Children]` do not apply to those classes. Method commands and global options continue to be discovered and filtered as before. A command type can appear only once in a definition.

A group has no command class and no action. Groups are shared with implicit groups from the method command model: a child command with the same ordinal name is reused, and aliases from the fluent group are added when missing. Method paths can therefore join a group defined in the fluent tree.

`WithName` changes the command name in the tree and does not change the read-only `StarterCommand.Name` property. A single command placed inside a group is not rooted onto the application root. Invalid tree definitions fail immediately with `ArgumentException` or `InvalidOperationException`. Name conflicts remain checked by `Starter.InstantiateCommands`, as described in [ADR 0010](0010-errors-and-exit-codes.md).

## Consequences

Applications can describe class-command placement next to their composition code, including groups without classes, names, and aliases. Existing discovery behavior remains available when `UseTree` is not used, and method commands and global options keep their existing discovery behavior when it is used.
