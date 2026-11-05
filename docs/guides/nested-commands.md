# Nested commands

This guide shows how a command ends up under another one, using the NestedCommands and DevOpsTool samples, and it points to the concept pages for the placement rules.

## Four ways to nest a command

A command can be nested in four ways: by the namespace hierarchy, with `[Parent<T>]` and `[Children]`, with groups and paths in the method model, or with an explicit tree built by `Starter.UseTree` (see [Fluent tree](fluent-tree.md)). A command with no parent is a top-level command.

## Nesting by namespace

If a class command is alone in its namespace, it becomes the parent of the commands in the deeper namespaces. In the NestedCommands sample, `Remote` is the only command in `Commands`, and `Add` and `Remove` live in `Commands.RemoteCommands`, so the commands are `remote add` and `remote remove`.

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
{
    public override Delegate HandlingMethod => (Action<string, string>)((name, url) =>
        Console.Out.WriteLine($"Added remote {name} ({url})"));
}
```

```powershell
dotnet run --project samples/CmdStarter.Samples.NestedCommands -- remote add origin https://example.com/repo.git
```

## Arguments after the full path

The leaf commands take their arguments positionally, after the full path. `remote add origin <url>` passes `origin` as `name` and the URL as `url`, and `remote remove origin` passes `origin` as `name`. In this sample, `Remote` and `Config` are empty classes, and here they only group their children.

```powershell
dotnet run --project samples/CmdStarter.Samples.NestedCommands -- remote remove origin
```

## Nesting with `[Parent<T>]`

`[Parent<T>]` on a class makes it a subcommand of `T`, wherever `T` is declared. Use it when the namespace hierarchy cannot give you the nesting you want. In the sample, `Config`, `Get` and `Set` share one namespace, so the namespace rule cannot nest them. `Get` and `Set` carry `[Parent<Config>]`, and they become `config get` and `config set`.

<!-- snippet: samples/CmdStarter.Samples.NestedCommands/Settings/Config.cs -->
```csharp
public sealed class Config : StarterCommand
{
}
```

<!-- snippet: samples/CmdStarter.Samples.NestedCommands/Settings/Set.cs -->
```csharp
[Parent<Config>]
public sealed class Set : StarterCommand
{
    public override Delegate HandlingMethod => (Action<string, string>)((key, value) =>
        Console.Out.WriteLine($"{key}={value}"));
}
```

`config set color blue` prints `color=blue`.

A plain command (a class marked `[Command]` that declares no `[Command]` method, with no base class and no interface) can be the parent or the child: `[Parent<T>]` on a plain class places it under any class command, plain or not, and `T` can itself be a plain class. See [Commands](../concepts/commands.md#plain-classes).

```powershell
dotnet run --project samples/CmdStarter.Samples.NestedCommands -- config set color blue
```

## Two class commands with the same name

Two class commands with the same name under the same parent are a configuration error: CmdStarter throws `InvalidAttributeException` when it builds the tree. Two `List` classes in different namespaces, both at the root, are one such case. `Starter.GroupsByNamespace` solves that case, as described in [Discovery](../concepts/discovery.md).

## Method model: a group and its methods

In the method model, a class marked `[Command("service")]` is a group, and each `[Command]` method in it is a subcommand. The method name gives the subcommand name, without an `Async` suffix: `DeployAsync` becomes `deploy`, and `Rollback` becomes `rollback`.

<!-- snippet: samples/CmdStarter.Samples.DevOpsTool/Commands/ServiceCommands.cs -->
```csharp
[Command("service")]
public sealed class ServiceCommands(IDeploymentClient client)
{
    // ...

    [Command]
    public async Task<int> DeployAsync(string name, DeployOptions options, [Option] string version = "latest")
    {
        // ...
    }

    [Command]
    public int Rollback(string name, DeployOptions options)
    {
        // ...
    }
}
```

```powershell
dotnet run --project samples/CmdStarter.Samples.DevOpsTool -- service deploy api --version 1.2.3
```

## Paths in names

A `[Command]` name can be a space-separated path, on a class (`[Command("admin users")]`) or on a method (`[Command("report daily")]`). Each segment that does not exist yet becomes an implicit group, a group with no action of its own. Existing commands and groups are reused.

<!-- snippet: CmdStarter.Tests.Commands/MethodGroups/MethodGroupCommands.cs -->
```csharp
[Command("admin users")]
public sealed class AdminUserCommands
{
    [Command]
    public static int Create() => 11;
}
```

<!-- snippet: CmdStarter.Tests.Commands/MethodGroups/MethodGroupCommands.cs -->
```csharp
public sealed class ReportCommands
{
    [Command("report daily")]
    public static int Daily() => 14;
}
```

## A method group under a class command

A method group can sit under a class command. Here `Tools` is a class command, and `[Command("tools cleanup")]` adds the `cleanup` group under it, so its method runs as `tools cleanup run`.

<!-- snippet: CmdStarter.Tests.Commands/MethodGroups/MethodGroupCommands.cs -->
```csharp
public sealed class Tools : StarterCommand
{
}

[Command("tools cleanup")]
public sealed class ToolsCleanupCommands
{
    [Command]
    public static int Run() => 13;
}
```

## An explicit group adopts an implicit one

An explicit group declared later takes over an implicit group of the same name. In this example, `zone area list` creates the `zone` group implicitly, and `[Command("zone")]` adopts it, so `zone info` sits in the same group. Two explicit groups on the same path throw `InvalidAttributeException`.

<!-- snippet: CmdStarter.Tests.Commands/MethodGroups/MethodGroupCommands.cs -->
```csharp
[Command("zone area")]
public sealed class AreaCommands
{
    [Command]
    public static int List() => 16;
}

[Command("zone")]
public sealed class ZoneCommands
{
    [Command]
    public static int Info() => 17;
}
```

## Mixing both models

Class commands and method commands can share one tree. In that case a lonely class command is not rooted; see [Commands](../concepts/commands.md#the-lonely-command).

## See also

- Examples: [NestedCommands](../../samples/CmdStarter.Samples.NestedCommands), [DevOpsTool](../../samples/CmdStarter.Samples.DevOpsTool).
- Neighbouring pages: [Fluent tree](fluent-tree.md), [Discovery](../concepts/discovery.md), [Commands](../concepts/commands.md), [Attributes](../concepts/attributes.md).
- ADR: [0004, class-based command model](../adr/0004-class-based-command-model.md), [0009, method-based command model](../adr/0009-method-based-command-model.md), [0017, namespace groups](../adr/0017-namespace-groups.md).
