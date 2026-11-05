# Fluent command tree

`Starter.UseTree` describes in code where each command class goes, instead of letting CmdStarter discover and place the classes.

## Define the tree

`Starter.UseTree(Action<CommandTreeDefinition>)` describes the placement of the class commands, and returns the `Starter`, so the calls chain. The callback receives the root level of the tree. In the FluentTree sample, `Users` has the child `ListUsers`, which has the alias `ls`, `Groups` has `ListGroups`, and `Reset` is in a group named `admin`:

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

## Add commands

`Add<TCommand>()` puts a command class at the current level. `Add<TCommand>(children)` puts it there and defines its children, inside the callback. `Add` returns the same level, so the calls chain. The class must implement `IStarterCommand`. `Add(Type)` and `Add(Type, children)` do the same with a `Type` instead of a type argument.

## Add groups

`Group(name, children)` adds a group that has no command class and no action. Its children go inside the callback, and groups can nest, so `admin users list` can come from nested groups. A group can also sit under a command class. Running a group on its own does not succeed.

## Rename and alias

In the callback of `Add<T>(children)` or `Group(name, children)`, `WithName` and `WithAliases` apply to that node. `WithName(name)` renames the command of the node, and the name must be a single segment, without white space. `WithAliases(params string[] aliases)` adds aliases to a command or to a group.

For example, `Status` is renamed to `state` and gets the alias `st`:

<!-- snippet: CmdStarter.Tests/FluentTree.cs -->
```csharp
starter.UseTree(tree => tree.Add<Status>(status => status.WithName("state").WithAliases("st")).Add<Users>());
```

## Builder and options shortcuts

`CmdStarterBuilder.UseTree(define)` configures the starter of a dependency-injection composition. `CmdStarterOptions.UseTree`, given to `AddCmdStarter`, defines the tree of the starter that the service provider resolves.

## What the tree replaces

With a definition, the class commands are exactly those of the definition, in the order written. CmdStarter does not scan for them, and `[Parent]` does not place them. A command class that is not in the definition is not a command, even if it carries `[Parent]`. A `Namespaces` filter does not remove the class commands of the tree, but method commands stay filtered by it.

Method commands are still discovered and filtered as usual. A method path can sit under a class command of the tree, or join a group defined in the tree. In this example, the method `Export` is placed under `users`:

<!-- snippet: CmdStarter.Tests.Commands/FluentTree/Methods/FluentMethodCommands.cs -->
```csharp
public static class FluentMethodCommands
{
    [Command("users export")]
    public static int Export() => 35;
}
```

A lonely command is still rooted, so its symbols go on the root command, unless it is placed inside a group.

See [Discovery](../concepts/discovery.md) for the ordinary discovery rules.

## Errors

Definition errors fail immediately, when `UseTree` is called. They are not `InvalidAttributeException`:

- `ArgumentException`: a type that is not a non-abstract class implementing `IStarterCommand`; a type added twice; a blank or multi-segment group name; a group with no command.
- `InvalidOperationException`: `WithName` on the root or on a group; any change to a definition kept after `UseTree` has returned.

If the callback throws, the previous tree stays in place.

See [ADR 0016](../adr/0016-fluent-command-tree.md).

## Run the sample

```powershell
dotnet run --project samples/CmdStarter.Samples.FluentTree -- users ls
dotnet run --project samples/CmdStarter.Samples.FluentTree -- groups list
dotnet run --project samples/CmdStarter.Samples.FluentTree -- admin reset
```

## See also

- Examples: [FluentTree](../../samples/CmdStarter.Samples.FluentTree).
- Neighbouring pages: [Nested commands](nested-commands.md), [Discovery](../concepts/discovery.md), [Commands](../concepts/commands.md), [Attributes](../concepts/attributes.md), [Errors and exit codes](errors-and-exit-codes.md).
- ADR: [0016, fluent command tree](../adr/0016-fluent-command-tree.md).
