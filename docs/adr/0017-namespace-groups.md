# 0017. Namespace Groups

Status: Accepted
Date: 2026-11-03

## Context

When class commands in different namespaces have the same command name, leaving them at the root creates a name conflict. For example, `App.Commands.Users.List` and `App.Commands.Groups.List` would both be named `list`. Jira CMD-18 asks for namespace segments to distinguish these commands in the command tree.

## Decision

`Starter.GroupsByNamespace` enables groups derived from command namespaces. It is `false` by default, so existing command trees keep their current placement unless an application opts in.

When enabled without a fluent tree, the base namespace is the longest common prefix, by dot-separated segments and ordinal comparison, of the namespaces of all class command types retained in `CommandsTypes`. Only direct children of `CommandsTypesTree` without a `ParentAttribute` (including a derived attribute or `[Parent(null)]`) are considered for namespace grouping. Their group path consists of the namespace segments after the base namespace, converted individually with `ToCommandName()` and without aliases. An empty path leaves the command at the root.

A root command that has a child in `CommandsTypesTree` whose namespace starts with the command's namespace followed by a dot represents its namespace. Its own final namespace segment is omitted from its group path, so it is placed up to its parent namespace. Commands already placed beneath a parent by `[Parent]`, `[Children]`, or the existing namespace hierarchy are not regrouped because they are not direct root children. Commands carrying any `ParentAttribute` are not grouped, even when `[Parent(null)]` leaves them at the root.

Namespace groups use the same placement mechanism as groups in [ADR 0016](0016-fluent-command-tree.md): an existing subcommand with the same ordinal name is reused; otherwise, an implicit group without an action is created. This allows a class command to represent a namespace group and lets method commands such as `[Command("users export")]` join that group through the method model's implicit groups described in [ADR 0009](0009-method-based-command-model.md).

The option is ignored when `ClassesBuildingMode` is `OnlyAttributes`, and a fluent tree supplied through `UseTree` takes precedence and ignores namespace grouping. Method commands continue to be discovered and added as usual; their paths can join groups created from class command namespaces.

## Consequences

Applications can opt into paths such as `users list` for `App.Commands.Users.List`, avoiding conflicts between same-named commands from different namespaces. The default remains unchanged, and applications using attribute-only placement or an explicit fluent tree keep those placement rules.
