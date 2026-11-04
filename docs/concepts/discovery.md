# Discovery and the command tree

This page explains how CmdStarter finds the class commands, how it filters them, in which order it sorts them, and how it places them in the command tree. Method commands are found in the same way, with the rules given in [Commands](commands.md).

## Where commands are searched

`Starter` searches the assemblies listed in `Starter.Assemblies`. When that list is empty, it searches every loaded assembly. For class commands, it keeps the non-abstract classes that implement `IStarterCommand` and have a namespace.

An assembly is searched in one of two ways. When it carries a `[CommandCatalog]` attribute and `Starter.UseCommandCatalogs` is `true` (the default), `Starter` reads the catalog that the attribute designates: the generator of the package writes one at compile time, and its types are the candidates, without a scan of the types of the assembly. Otherwise, the assembly is scanned by reflection. The predicate above applies to the types of both sources. See [Source generator](../advanced/source-generator.md).

The filters run in this order:

1. the assemblies;
2. `Starter.Namespaces`;
3. `Starter.Classes`.

When a filter leaves no command, `Starter` throws `NoCommandFoundException`. Its `LastFilterApplied` property names the filter that emptied the list: `Assemblies`, `Namespaces` or `Classes`.

## Filtering by namespace

`Starter.Namespaces` is a list of namespace **prefixes**. A type is kept when its namespace starts with one of the entries, so the commands of the child namespaces are kept too. The comparison is ordinal and works on the text, not on whole segments: `App.Users` also keeps the namespace `App.UsersExtra`.

- An empty list, a `null` value, and empty or blank entries mean no filter. Setting `Namespaces` to `null` gives an empty list.
- An entry that starts with `~` (`Starter.ExclusionSymbol`) excludes every namespace that starts with the text that follows it.
- When the list holds only exclusions, every other namespace is kept.

The following line keeps only the commands of the namespace of `AdminCommands`, from the [ReplServer](../../samples/CmdStarter.Samples.ReplServer) sample:

<!-- snippet: samples/CmdStarter.Samples.ReplServer/Program.cs -->
```csharp
Namespaces = [typeof(AdminCommands).Namespace!]
```

## Filtering by class name

`Starter.Classes` filters on the full type name. An entry starting with `~` excludes the matching types, and when the list holds only exclusions, every other type is kept.

The pattern is matched against the **end** of the full name. It must start at the beginning of the name or just after a dot. A pattern without wildcard therefore matches a trailing dotted part of the full name: `IO.Starter` matches `IO.Starter` and `A.IO.Starter`.

Wildcards are translated into regular expressions before matching:

| Symbol | Matches | Meaning |
|---|---|---|
| `?` | `\w` | exactly one letter, digit or underscore; never a dot |
| `??` | `.` | exactly one character, a dot included |
| `*` | `\w*` | any number of letters, digits or underscores; never a dot |
| `**` | `.*` | any number of characters, dots included |
| `~` (first character of an entry) | nothing | excludes what the rest of the entry matches |

The symbols are the constants `Starter.ExclusionSymbol`, `AnyCharSymbol`, `AnyCharSymbolIncludeDots`, `MultiAnyCharSymbol` and `MultiAnyCharSymbolIncludeDots`.

As an example, on the twelve types of the test set of `Basics.ClassesFilterRegexTest` (written here without their common prefix), the pattern `Starter?` keeps `StarterA` and `StarterB`, and the pattern `IO.Starter` keeps `IO.Starter`, `A.IO.Starter`, `B.IO.Starter` and `AB.IO.Starter`.

## Order

The result of discovery is sorted by full type name, with an ordinal comparison. The global options types are sorted the same way. The tree and the help therefore do not depend on the order in which reflection lists the types. With `UseTree`, the order is the order of the definition.

## When the commands are rebuilt

- Changing `Namespaces`, `Classes`, `Assemblies` or `UseCommandCatalogs` resets the commands and the tree, and the discovery runs again.
- Changing `ClassesBuildingMode`, `IsRootingLonelyCommand` or `GroupsByNamespace` resets the tree.
- Setting `CommandsTypes` resets the tree, and the list is checked first: a type that is not a non-abstract class implementing `IStarterCommand`, that has no namespace or that is listed twice raises an `ArgumentException` that names the type, and nothing changes. `null` means an empty list.
  - Before the first discovery, a non-empty list stands for the discovery of the class commands: the discovery does not run, so there are no method commands.
  - After the discovery, only the class commands change and the discovered method commands stay.
  - An empty list goes back to the discovery.

## Building the tree

The tree is built from three sources, selected by `Starter.ClassesBuildingMode`:

| Mode | Source |
|---|---|
| `Both` (the default) | the `[Parent]` and `[Children]` attributes, then the namespace hierarchy |
| `OnlyAttributes` | the `[Parent]` and `[Children]` attributes only |
| `OnlyNamespaces` | the namespace hierarchy only |

A command left without a parent becomes a top-level command.

### `[Parent]`

`[Parent<T>]` (or `[Parent(typeof(T))]`) on a class makes it a subcommand of `T`. A class can have only one parent attribute. Two parent attributes on one class throw `InvalidAttributeException`, and a loop of parents throws `LoopException`.

<!-- snippet: samples/CmdStarter.Samples.NestedCommands/Settings/Get.cs -->
```csharp
[Parent<Config>]
public sealed class Get : StarterCommand
{
    public override Delegate HandlingMethod => (Action<string>)(key => Console.Out.WriteLine($"{key}=default"));
}
```

### `[Children]`

`[Children<T>]`, `[Children("Some.Namespace")]` or `[Children(typeof(T))]` on a class makes the commands of the given namespace subcommands of that class, unless such a command has a `[Parent]` attribute. The attribute can be repeated. See [Attributes](attributes.md).

### The namespace hierarchy

A command becomes the parent of the commands in deeper namespaces when it is the **only** command in its own namespace. Every command whose namespace starts with that namespace followed by a dot becomes one of its children. A command that is already placed by `[Parent]` is not moved in `Both` mode.

In the NestedCommands sample, `Remote` is the only command in its namespace, and `Add` is in the deeper namespace below it. The result is the command `remote add`:

<!-- snippet: samples/CmdStarter.Samples.NestedCommands/Commands/Remote.cs -->
```csharp
namespace CyberInternauts.CmdStarter.Samples.NestedCommands.Commands;

public sealed class Remote : StarterCommand
{
}
```

<!-- snippet: samples/CmdStarter.Samples.NestedCommands/Commands/RemoteCommands/Add.cs -->
```csharp
namespace CyberInternauts.CmdStarter.Samples.NestedCommands.Commands.RemoteCommands;

public sealed class Add : StarterCommand
```

## `UseTree`: describing the tree in code

`Starter.UseTree(Action<CommandTreeDefinition>)` replaces the discovery and the placement of class commands. The types listed in the definition are the class commands. There is no assembly scan and no `Namespaces` or `Classes` filter, and `[Parent]`, `[Children]` and `ClassesBuildingMode` are ignored. Method commands and global options are still discovered and filtered.

In the FluentTree sample, the tree is written in code:

<!-- snippet: samples/CmdStarter.Samples.FluentTree/Program.cs -->
```csharp
return await new Starter()
    .UseTree(tree => tree
        .Add<Users>(users => users
            .Add<ListUsers>(list => list.WithAliases("ls")))
        .Add<Groups>(groups => groups
            .Add<ListGroups>())
        .Group("admin", admin => admin
            .Add<Reset>()))
    .Start(args);
```

The definition itself is described in [Fluent tree](../guides/fluent-tree.md).

## `GroupsByNamespace`: namespaces as groups

`Starter.GroupsByNamespace` (default `false`) places top-level class commands in groups made from their namespaces. Without it, `Users.List` and `Groups.List` would both be a `list` command at the root, which is a name conflict. With it, they become `users list` and `groups list`.

<!-- snippet: CmdStarter.Tests/NamespaceGroups.cs -->
```csharp
starter.GroupsByNamespace = true;
```

The rule is the one of [ADR 0017](../adr/0017-namespace-groups.md):

1. The **base namespace** is the longest common prefix, by dot-separated segments, of the namespaces of all the class command types kept by discovery.
2. Only commands placed directly under the root, and without any parent attribute, are grouped.
3. The group path of a command is its namespace segments after the base namespace, each converted to kebab-case. An empty path leaves the command at the root.
4. A top-level command that has a child in its own namespace represents that namespace: its last segment is left out of its path. A command in the `remote` namespace that represents it is therefore `remote prune`, not `remote remote`.
5. A group reuses an existing subcommand of the same name. Otherwise, an implicit group (with no handler) is created. Method commands such as `[Command("users export")]` can join these groups.
6. The option is ignored when `ClassesBuildingMode` is `OnlyAttributes`, and when `UseTree` is used.

## Lonely commands and groups

A command is rooted as a lonely command only when it is the only command and has no group path. A command that `UseTree` places in a group is not rooted. See [Commands](commands.md) for the rule of lonely commands.

## See also

- Examples: [NestedCommands](../../samples/CmdStarter.Samples.NestedCommands), [FluentTree](../../samples/CmdStarter.Samples.FluentTree).
- Neighbouring pages: [Commands](commands.md), [Attributes](attributes.md), [Nested commands](../guides/nested-commands.md), [Fluent tree](../guides/fluent-tree.md), [Source generator](../advanced/source-generator.md).
- ADRs: [0004, class-based command model](../adr/0004-class-based-command-model.md), [0016, fluent command tree](../adr/0016-fluent-command-tree.md), [0017, namespace groups](../adr/0017-namespace-groups.md), [0027, command catalog](../adr/0027-command-catalog.md).
